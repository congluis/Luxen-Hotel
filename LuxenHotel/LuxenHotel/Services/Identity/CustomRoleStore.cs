using LuxenHotel.Data;
using LuxenHotel.Models.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class CustomRoleStore : IRoleStore<Role>
{
    private readonly ApplicationDbContext _context;

    public CustomRoleStore(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IdentityResult> CreateAsync(Role role, CancellationToken cancellationToken)
    {
        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(Role role, CancellationToken cancellationToken)
    {
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<Role?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        return await _context.Roles.FindAsync(new object[] { roleId }, cancellationToken);
    }

    public async Task<Role?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRoleName, cancellationToken);
    }

    public Task<string?> GetNormalizedRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.NormalizedName);
    }

    public Task<string> GetRoleIdAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Id);
    }

    public Task<string?> GetRoleNameAsync(Role role, CancellationToken cancellationToken)
    {
        return Task.FromResult(role.Name);
    }

    public Task SetNormalizedRoleNameAsync(Role role, string? normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public Task SetRoleNameAsync(Role role, string? roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> UpdateAsync(Role role, CancellationToken cancellationToken)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public void Dispose()
    {
        // No-op
    }
}