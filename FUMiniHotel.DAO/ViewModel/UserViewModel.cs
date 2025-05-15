using System.ComponentModel.DataAnnotations;

namespace FUMiniHotel.DAO.ViewModel
{
    public class UserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^0\d{9}$", ErrorMessage = "Phone number must be exactly 10 digits and start with a 0 (e.g., 0123456789).")]
        public string PhoneNumber { get; set; }

        public string Password { get; set; }

        public List<string> Roles { get; set; } = new List<string>();

        public string SelectedRole { get; set; }
    }
}