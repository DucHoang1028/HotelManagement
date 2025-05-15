using Microsoft.AspNetCore.Identity;
using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.Repositories.IRepositories;
using FUMiniHotel.Shared;
using System.Threading.Tasks;
using System.Linq;

namespace FUMiniHotel.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountRepository(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task CreateRoleAsync(Role role)
        {
            var roleName = role.ToRoleString();
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public async Task<bool> RoleExistsAsync(Role role)
        {
            return await _roleManager.RoleExistsAsync(role.ToRoleString());
        }

        public async Task AssignRoleToUserAsync(ApplicationUser user, Role role)
        {
            var roleName = role.ToRoleString();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
            }
        }

        public async Task RemoveRoleFromUserAsync(ApplicationUser user, Role role)
        {
            var roleName = role.ToRoleString();
            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
            }
        }

        public async Task<List<Role>> GetRolesForUserAsync(ApplicationUser user)
        {
            var roleNames = await _userManager.GetRolesAsync(user);
            return roleNames.Select(r => RoleHelper.FromRoleString(r)).ToList();
        }

        public Task<List<Role>> GetAllRolesAsync()
        {
            return Task.FromResult(RoleHelper.GetAllRoles().ToList());
        }
    }
}