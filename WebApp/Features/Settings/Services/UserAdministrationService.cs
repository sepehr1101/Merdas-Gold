using MerdasGold.Features.Persistence;
using MerdasGold.Features.Settings.Models;
using Microsoft.EntityFrameworkCore;

namespace MerdasGold.Features.Settings.Services;

public sealed class UserAdministrationService(IDbContextFactory<MerdasGoldDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.UserName)
            .Select(user => new UserListItem
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = (user.FirstName + " " + user.LastName).Trim(),
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Email = user.Email ?? string.Empty,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > now
            })
            .ToListAsync(cancellationToken);

        var roleAssignments = await (
                from userRole in dbContext.UserRoles.AsNoTracking()
                join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                select new { userRole.UserId, RoleName = role.Name ?? string.Empty })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleAssignments
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => string.Join("، ", group.Select(item => TranslateRole(item.RoleName))));

        foreach (var user in users)
        {
            user.Roles = rolesByUser.GetValueOrDefault(user.Id, "بدون نقش");
        }

        return users;
    }

    private static string TranslateRole(string roleName) => roleName switch
    {
        "Administrator" => "مدیر سیستم",
        _ => roleName
    };
}
