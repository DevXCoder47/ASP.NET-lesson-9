using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeacherManagement.DTOs;

namespace StudentTeacherManagement.Controllers;

[ApiController]
[Route("groups")]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IMapper _mapper;

    public GroupController(IGroupService groupService, IMapper mapper)
    {
        _groupService = groupService;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Student, Teacher")]
    public async Task<ActionResult<IEnumerable<GroupDTO>>> GetGroups([FromQuery] string? name = null,
                                                                  [FromQuery] int skip = 0,
                                                                  [FromQuery] int take = 10)
    {
        var groups = await _groupService.GetGroups(name, skip, take);
        Console.WriteLine("endpoint");
        return Ok(_mapper.Map<IEnumerable<GroupDTO>>(groups));
    }
    
    [HttpGet("{id}")]
    [Authorize(Roles = "Student, Teacher")]
    public async Task<ActionResult<GroupDTO>> GetGroupById([FromRoute] Guid id)
    {
        try
        {
            var group = await _groupService.GetGroupById(id);
            if (group == null)
                throw new ArgumentException("Such group doesn't exist");
            return Ok(_mapper.Map<GroupDTO>(group));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteGroup([FromRoute] Guid id)
    {
        try
        {
            await _groupService.DeleteGroup(id);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPost]
    public async Task<ActionResult<GroupDTO>> AddGroup([FromBody] CreateGroupDTO createGroupDto)
    {
        try
        {
            var group = await _groupService.AddGroup(_mapper.Map<Group>(createGroupDto));
            return Created($"groups/{group.Id}", _mapper.Map<GroupDTO>(group));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpPatch("add_student/{studentId}/to_group/{groupId}")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult> AddStudentToGroup([FromRoute] Guid groupId, [FromRoute] Guid studentId)
    {
        try
        {
            await _groupService.AddStudentToGroup(groupId, studentId);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}