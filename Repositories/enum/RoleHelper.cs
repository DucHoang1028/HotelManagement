using FUMiniHotel.Repositories;
using FUMiniHotel.Repositories.IRepositories;

namespace FUMiniHotel.Shared
{
    public static class RoleHelper
    {
        // Role string constants for use in [Authorize] and database
        public const string Customer = "Customer";
        public const string HotelOwner = "Hotel Owner";
        public const string Staff = "Staff";
        public const string Admin = "Admin";

        public static string ToRoleString(this Role role)
        {
            return role switch
            {
                Role.Customer => Customer,
                Role.HotelOwner => HotelOwner,
                Role.Staff => Staff,
                Role.Admin => Admin,
                _ => throw new ArgumentException($"Unknown role: {role}")
            };
        }

        public static Role FromRoleString(string roleString)
        {
            return roleString switch
            {
                Customer => Role.Customer,
                HotelOwner => Role.HotelOwner,
                Staff => Role.Staff,
                Admin => Role.Admin,
                _ => throw new ArgumentException($"Unknown role string: {roleString}")
            };
        }

        public static IEnumerable<Role> GetAllRoles()
        {
            return Enum.GetValues(typeof(Role)).Cast<Role>();
        }
    }
}