using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeacherManagement.DTOs;

namespace StudentTeacherManagement.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
   private readonly IAuthService _authService;
   private readonly IMapper _mapper;

   public AuthController(IAuthService authService, IMapper mapper)
   {
      _authService = authService;
      _mapper = mapper;
   }

   [HttpPost("register")]
   public async Task<ActionResult> Register([FromBody] RegisterDTO user)
   {
        try
        {
            switch (user.Role) {
                case "Student": await _authService.Register(_mapper.Map<Student>(user)); break;
                case "Teacher": await _authService.Register(_mapper.Map<Teacher>(user)); break;
                default: throw new ArgumentException("Role is invalid");
            }
            return Ok();
        }
        catch(ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
   }
   [HttpPost("verifyUser")]
   public async Task<ActionResult<AuthResponse>> VerifyUser([FromBody] VerificationData verificationData)
   {
      var (user, token) = await _authService.ValidateAccount(verificationData.Email, verificationData.Code);
      return Ok(new AuthResponse(){ User = user, Token = token });
   }

   [HttpPost("login")]
   public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginDTO user)
   {
        try
        {
            var (loggedUser, token) = await _authService.Login(user.Email, user.Password);
            return Ok(new AuthResponse() { User = loggedUser, Token = token });
        }
        catch(ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
   }
    [HttpPost("forgot_password")]
    public async Task<ActionResult<VerificationData>> RequestPasswordReset([FromBody] RequestPasswordResetData data)
    {
        try
        {
            var (email, code) = await _authService.GenerateResetPasswordCode(data.Email);
            return Ok(new VerificationData() { Email = email, Code = code});
        }
        catch(ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPatch("reset_password")]
    public async Task<ActionResult> ResetPassword([FromBody] PasswordResetData resetData)
    {
        try
        {
            await _authService.ResetPassword((resetData.Code, resetData.Email, resetData.NewPassword));
            return Ok();
        }
        catch(ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}