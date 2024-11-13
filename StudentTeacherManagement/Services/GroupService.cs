using Microsoft.EntityFrameworkCore;
using StudentTeacherManagement.Core.Interfaces;
using StudentTeacherManagement.Core.Models;
using StudentTeaherManagement.Storage;

namespace StudentTeacherManagement.Services;

public class GroupService : IGroupService
{
    private readonly DataContext _context;

    public GroupService(DataContext context)
    {
        _context = context;
    }

    #region DQL

    public async Task<IEnumerable<Group>> GetGroups(string? name, int skip, int take, CancellationToken cancellationToken = default)
    {
        var groups = _context.Groups.AsQueryable();

        if (!string.IsNullOrEmpty(name))
        {
            groups = groups.Where(g => g.Name.Contains(name));
        }

        return await groups.OrderBy(g => g.Name)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Group?> GetGroupById(Guid id, CancellationToken cancellationToken = default)
    {
        var groups = _context.Groups.AsQueryable();
        var group = await groups.FirstOrDefaultAsync(g => g.Id == id);
        return group;
    }

    #endregion

    #region DML

    public async Task<Group> AddGroup(Group group, CancellationToken cancellationToken = default)
    {
        if (group == null)
            throw new ArgumentException("Group must not be null");
        await _context.Groups.AddAsync(group, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return group;
    }

    public async Task DeleteGroup(Guid id, CancellationToken cancellationToken = default)
    {
        var groups = _context.Groups.AsQueryable();
        var group = groups.FirstOrDefault(g => g.Id == id);
        if (group == null)
            throw new ArgumentException("Such group doesn't exist");
        _context.Groups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddStudentToGroup(Guid groupId, Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students.FindAsync([studentId], cancellationToken: cancellationToken);
        student.GroupId = groupId;
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}