namespace StudentTeacherManagement.DTOs
{
    public class PasswordResetData
    {
        public string Email { get; set; }
        public int Code { get; set; }
        public string NewPassword { get; set; }
    }
}
