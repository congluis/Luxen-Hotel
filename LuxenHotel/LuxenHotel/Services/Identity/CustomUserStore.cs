using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LuxenHotel.Services.Identity;

public class CustomUserStore :
        IUserStore<User>,
        IUserPasswordStore<User>,
        IUserEmailStore<User>,
        IUserRoleStore<User>
{
    private readonly ApplicationDbContext _context;

    public CustomUserStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
    }

    public async Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedUserName);
    }

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Id);
    }

    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUserName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.UserName = userName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    // Password methods
    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash;
        return Task.CompletedTask;
    }

    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash);
    }

    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.PasswordHash != null);
    }

    // Email methods
    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.Email = email;
        return Task.CompletedTask;
    }

    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.Email);
    }

    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken)
    {
        // Always return true since we don't use EmailConfirmed
        return Task.FromResult(true);
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        // No-op since we don't use EmailConfirmed
        return Task.CompletedTask;
    }

    public async Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken)
    {
        return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmail = normalizedEmail;
        return Task.CompletedTask;
    }

    // Role methods
    public async Task AddToRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role != null)
        {
            var userRole = new IdentityUserRole<string>
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveFromRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role != null)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<IList<string>> GetRolesAsync(User user, CancellationToken cancellationToken)
    {
        var query = from userRole in _context.UserRoles
                    join role in _context.Roles on userRole.RoleId equals role.Id
                    where userRole.UserId == user.Id
                    select role.Name;

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<bool> IsInRoleAsync(User user, string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role != null)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken);
        }
        return false;
    }

    public async Task<IList<User>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName, cancellationToken);
        if (role != null)
        {
            var userIds = await _context.UserRoles
                .Where(ur => ur.RoleId == role.Id)
                .Select(ur => ur.UserId)
                .ToListAsync(cancellationToken);

            return await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(cancellationToken);
        }
        return new List<User>();
    }

    public void Dispose()
    {
        // No-op
    }
}
