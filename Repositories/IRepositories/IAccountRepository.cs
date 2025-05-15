using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.Repositories;
using System.Data;
using System.Threading.Tasks;

namespace FUMiniHotel.Repositories.IRepositories
{
    public interface IAccountRepository
    {
        Task CreateRoleAsync(Role role);
        Task<bool> RoleExistsAsync(Role role);
        Task AssignRoleToUserAsync(ApplicationUser user, Role role);
        Task RemoveRoleFromUserAsync(ApplicationUser user, Role role);
        Task<List<Role>> GetRolesForUserAsync(ApplicationUser user);
        Task<List<Role>> GetAllRolesAsync();
    }
}