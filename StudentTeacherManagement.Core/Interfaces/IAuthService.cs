using StudentTeacherManagement.Core.Models;

namespace StudentTeacherManagement.Core.Interfaces;

public interface IAuthService
{
    Task Register(User user);
    Task<(User?, string)> Login(string email, string password);

    Task<(User, string)> ValidateAccount(string email, int code);
    Task<(string, int)> GenerateResetPasswordCode(string email);
    Task ResetPassword((int code, string email, string newPassword) passwordResetData);
}