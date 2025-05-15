using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.Repositories.IRepositories;
using System.Threading.Tasks;
using System.Linq;
using FUMiniHotel.Shared;
using FUMiniHotel.DAO.ViewModel;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FUMiniHotel.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IAccountRepository _accountRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            IAccountRepository accountRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _accountRepository = accountRepository;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Manage()
        {
            var users = _userManager.Users.ToList();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _accountRepository.GetRolesForUserAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Roles = roles.Select(r => r.ToRoleString()).ToList()
                });
            }

            return View(userViewModels);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _accountRepository.GetRolesForUserAsync(user);
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.Select(r => r.ToRoleString()).ToList()
            };

            return View(userViewModel);
        }

        public async Task<IActionResult> Create()
        {
            var allRoles = RoleHelper.GetAllRoles();
            foreach (var role in allRoles)
            {
                var roleName = role.ToRoleString();
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var roles = _roleManager.Roles.ToList();
            if (!roles.Any())
            {
                roles = allRoles.Select(r => new IdentityRole(r.ToRoleString())).ToList();
            }

            if (User.IsInRole("Admin"))
            {
                roles = roles.Where(r => r.Name != "Admin").ToList();
            }

            ViewBag.Roles = roles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToList();

            return View(new UserViewModel());
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserViewModel userViewModel)
        {
            if (User.IsInRole("Admin") && userViewModel.SelectedRole == "Admin")
            {
                ModelState.AddModelError("", "Admins cannot assign the Admin role to other users.");
            }

            // Check if the phone number is unique
            if (!string.IsNullOrEmpty(userViewModel.PhoneNumber))
            {
                var existingUserWithPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == userViewModel.PhoneNumber);
                if (existingUserWithPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "This phone number is already in use.");
                }
            }

            if (ModelState.IsValid)
            {
                var userId = Guid.NewGuid().ToString();
                var user = new ApplicationUser
                {
                    Id = userId,
                    UserName = userViewModel.Email,
                    Email = userViewModel.Email,
                    FirstName = userViewModel.FirstName,
                    LastName = userViewModel.LastName,
                    PhoneNumber = userViewModel.PhoneNumber,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, userViewModel.Password ?? "DefaultPassword123!");
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(userViewModel.SelectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, userViewModel.SelectedRole);
                    }
                    return RedirectToAction(nameof(Manage));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var allRoles = _roleManager.Roles.ToList();
            if (!allRoles.Any())
            {
                allRoles = RoleHelper.GetAllRoles().Select(r => new IdentityRole(r.ToRoleString())).ToList();
            }
            if (User.IsInRole("Admin"))
            {
                allRoles = allRoles.Where(r => r.Name != "Admin").ToList();
            }

            ViewBag.Roles = allRoles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToList();
            return View(userViewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _accountRepository.GetRolesForUserAsync(user);
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.Select(r => r.ToRoleString()).ToList(),
                SelectedRole = roles.Select(r => r.ToRoleString()).FirstOrDefault()
            };

            var allRoles = _roleManager.Roles.ToList();
            if (User.IsInRole("Admin"))
            {
                allRoles = allRoles.Where(r => r.Name != "Admin").ToList();
            }

            ViewBag.Roles = allRoles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name,
                Selected = userViewModel.Roles.Contains(r.Name)
            }).ToList();

            return View(userViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserViewModel userViewModel)
        {
            if (id != userViewModel.Id)
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            if (User.IsInRole("Admin") && userViewModel.SelectedRole == "Admin")
            {
                ModelState.AddModelError("", "Admins cannot assign the Admin role to other users.");
            }

            // Check if the phone number is unique (excluding the current user)
            if (!string.IsNullOrEmpty(userViewModel.PhoneNumber))
            {
                var existingUserWithPhone = await _userManager.Users
                    .FirstOrDefaultAsync(u => u.PhoneNumber == userViewModel.PhoneNumber && u.Id != user.Id);
                if (existingUserWithPhone != null)
                {
                    ModelState.AddModelError("PhoneNumber", "This phone number is already in use by another user.");
                }
            }

            if (ModelState.IsValid)
            {
                user.FirstName = userViewModel.FirstName;
                user.LastName = userViewModel.LastName;
                user.PhoneNumber = userViewModel.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    var currentRoles = await _userManager.GetRolesAsync(user);
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrEmpty(userViewModel.SelectedRole))
                    {
                        await _userManager.AddToRoleAsync(user, userViewModel.SelectedRole);
                    }
                    return RedirectToAction(nameof(Manage));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            var allRoles = _roleManager.Roles.ToList();
            if (User.IsInRole("Admin"))
            {
                allRoles = allRoles.Where(r => r.Name != "Admin").ToList();
            }
            ViewBag.Roles = allRoles.Select(r => new SelectListItem
            {
                Value = r.Name,
                Text = r.Name
            }).ToList();
            return View(userViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Manage));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            var roles = await _accountRepository.GetRolesForUserAsync(user);
            var userViewModel = new UserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Roles = roles.Select(r => r.ToRoleString()).ToList()
            };

            return View("Manage", userViewModel);
        }
    }
}