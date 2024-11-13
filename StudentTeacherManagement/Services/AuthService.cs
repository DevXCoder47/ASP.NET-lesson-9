using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeacherManagement.DTOs;
using StudentTeaherManagement.Storage;

namespace StudentTeacherManagement.Services;

public class AuthService : IAuthService
{
    private const string PasswordPattern = @"^((?=\S*?[A-Z])(?=\S*?[a-z])(?=\S*?[0-9]).{6,})\S$";
    private const string EmailPattern = @"^[\w\.-]+@[a-zA-Z\d\.-]+\.[a-zA-Z]{2,6}$";
    private const int MinCodeValue = 100_000;
    private const int MaxCodeValue = 1_000_000;
    private static TimeSpan MaxVerificationTime => TimeSpan.FromMinutes(10);
    
    private readonly DataContext _context;
    private readonly IEmailSender _emailSender;
    private IConfiguration _configuration;

    private static IDictionary<int, User> _unverifiedUsers = new Dictionary<int, User>();
    private static IDictionary<string, int> _dataForPasswordResets = new Dictionary<string, int>();
    public AuthService(DataContext context, IEmailSender emailSender, IConfiguration configuration)
    {
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
    }

    public Task Register(User user)
    {
        // validation
        ValidateUser(user);
        
        // generate code
        var code = new Random().Next(MinCodeValue, MaxCodeValue);
        user.CreatedAt = DateTime.UtcNow;
        _unverifiedUsers.Add(code, user);
        
        // send to email
        return _emailSender.Send("Your code: " + code);
    }

    private void ValidateUser(User user)
    {
        if (string.IsNullOrEmpty(user.FirstName))
        {
            throw new ArgumentException("First name is invalid", nameof(user.FirstName));
        }
        if (string.IsNullOrEmpty(user.LastName))
        {
            throw new ArgumentException("Last name is invalid", nameof(user.LastName));
        }
        if (user.DateOfBirth > DateTime.Now)
        {
            throw new ArgumentException("Date of birth is invalid", nameof(user.DateOfBirth));
        }
        if (!Regex.IsMatch(user.Email, EmailPattern))
        {
            throw new ArgumentException("Email is invalid", nameof(user.Email));
        }   
        if (!Regex.IsMatch(user.Password, PasswordPattern))
        {
            throw new ArgumentException("Password is invalid", nameof(user.Password));
        }  
        if(user.Role != "Student" && user.Role != "Teacher")
        {
            throw new ArgumentException("Role is invalid", nameof(user.Role));
        }
    }

    public async Task<(User?, string)> Login(string email, string password)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Email.Equals(email) && s.Password.Equals(password));
        if(student != null)
            return (student, GenerateToken(student.Role));
        var teacher = await _context.Teachers.SingleOrDefaultAsync(t => t.Email.Equals(email) && t.Password.Equals(password));
        if (teacher != null)
            return (teacher, GenerateToken(teacher.Role));
        throw new ArgumentException("Invalid email or password");
    }

    public async Task<(User, string)> ValidateAccount(string email, int code)
    {
        // check code with email
        if (_unverifiedUsers.TryGetValue(code, out var unverifiedUser))
        {
            if (unverifiedUser.Email.Equals(email) && (DateTime.UtcNow - unverifiedUser.CreatedAt) < MaxVerificationTime)
            {
                switch (unverifiedUser.Role)
                {
                    case "Student":
                        var student = new Student()
                        {
                            FirstName = unverifiedUser.FirstName,
                            LastName = unverifiedUser.LastName,
                            Email = unverifiedUser.Email,
                            Password = unverifiedUser.Password,
                            Role = unverifiedUser.Role,
                            DateOfBirth = unverifiedUser.DateOfBirth,
                            CreatedAt = DateTime.UtcNow,
                        };
                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();
                        return (student, GenerateToken(student.Role));
                    case "Teacher":
                        var teacher = new Teacher()
                        {
                            FirstName = unverifiedUser.FirstName,
                            LastName = unverifiedUser.LastName,
                            Email = unverifiedUser.Email,
                            Password = unverifiedUser.Password,
                            Role = unverifiedUser.Role,
                            DateOfBirth = unverifiedUser.DateOfBirth,
                            CreatedAt = DateTime.UtcNow,
                        };
                        _context.Teachers.Add(teacher);
                        await _context.SaveChangesAsync();
                        return (teacher, GenerateToken(teacher.Role));
                } 
            }
        }
        throw new ArgumentException("Code or email is incorrect");
    }
    public async Task<(string, int)> GenerateResetPasswordCode(string email)
    {
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Email == email);
        var teacher = student == null
        ? await _context.Teachers.SingleOrDefaultAsync(t => t.Email == email)
        : null;
        if (student == null && teacher == null)
            throw new ArgumentException("email not registered");
        var code = new Random().Next(MinCodeValue, MaxCodeValue);
        _dataForPasswordResets[email] = code;
        await _emailSender.Send($"Your password reset code: {code}");
        return (email, code);
    }
    private string GenerateToken(string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration.GetSection("AppSettings:Token").Value!));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration.GetSection("AppSettings:ExpireTime").Value!)),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    public async Task ResetPassword((int code, string email, string newPassword) passwordResetData)
    {
        if (!_dataForPasswordResets.ContainsKey(passwordResetData.email))
            throw new ArgumentException("Wrong email");
        if (_dataForPasswordResets[passwordResetData.email] != passwordResetData.code)
            throw new ArgumentException("Wrong code");
        if(!Regex.IsMatch(passwordResetData.newPassword, PasswordPattern))
            throw new ArgumentException("Password is invalid");
        var student = await _context.Students.SingleOrDefaultAsync(s => s.Email == passwordResetData.email);
        if(student != null)
            student.Password = passwordResetData.newPassword;
        else
        {
            var teacher = await _context.Teachers.SingleAsync(t => t.Email == passwordResetData.email);
            teacher.Password = passwordResetData.newPassword;
        }
        await _context.SaveChangesAsync();
        _dataForPasswordResets.Remove(passwordResetData.email);
    }
}