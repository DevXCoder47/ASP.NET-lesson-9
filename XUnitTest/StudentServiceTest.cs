using StudentTeacherManagement.Services;
using StudentTeaherManagement.Storage;

namespace XUnitTest
{
    public class StudentServiceTest
    {
        [Fact]
        public async Task GetStudents_CorrectData_ReturnStudents()
        {
            var context = new DataContext();
            var studentService = new StudentService(context);
            var students = await studentService.GetStudents(0, 10);
            Assert.Equal(10, students.Count());
        }
    }
}