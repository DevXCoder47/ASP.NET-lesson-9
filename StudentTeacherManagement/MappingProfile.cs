using AutoMapper;
using StudentTeacherManagement.Core.Models;
using StudentTeacherManagement.DTOs;

namespace StudentTeacherManagement;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Group, GroupDTO>().ReverseMap();
        CreateMap<Group, CreateGroupDTO>().ReverseMap();
        CreateMap<Student, CreateStudentDTO>().ReverseMap();
        CreateMap<Student, StudentDTO>().ReverseMap();
        CreateMap<Teacher, RegisterDTO>().ReverseMap();

        CreateMap<Student, RegisterDTO>().ReverseMap();
    }
}