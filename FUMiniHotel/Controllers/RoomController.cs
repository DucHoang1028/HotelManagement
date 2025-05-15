using System.Security.Claims;
using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.DAO;
using FUMiniHotel.DAO.ViewModel;
using FUMiniHotel.Repositories.IRepositories;
using FUMiniHotel.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using X.PagedList;

namespace FUMiniHotel.Controllers
{
    public class RoomController : Controller
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IFileService _fileService;
        private readonly UserManager<ApplicationUser> _userManager; // Add UserManager to get Hotel Owners

        public RoomController(
            IRoomRepository roomRepository,
            IFileService fileService,
            IBookingRepository bookingRepository,
            UserManager<ApplicationUser> userManager) // Inject UserManager
        {
            _bookingRepository = bookingRepository;
            _roomRepository = roomRepository;
            _fileService = fileService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(DateTime? checkIn, DateTime? checkOut, string searchAddress, int? roomTypeFilter, string searchHeadline, int? numberOfCustomers, int? maxCapacityFilter, int page = 1, int pageSize = 9)
        {
            var rooms = await _roomRepository.GetAllRoomAsync();
            var roomTypes = await _roomRepository.GetAllRoomTypeAsync();

            // Populate ViewBag.RoomType for the dropdowns
            ViewBag.RoomType = roomTypes.Select(rt => new SelectListItem
            {
                Value = rt.RoomTypeID.ToString(),
                Text = rt.RoomTypeName
            }).ToList();

            // Populate ViewBag.MaxCapacities for the max capacity filter dropdown
            ViewBag.MaxCapacities = rooms
                .Select(r => r.RoomMaxCapacity)
                .Distinct()
                .OrderBy(cap => cap)
                .Select(cap => new SelectListItem
                {
                    Value = cap.ToString(),
                    Text = cap.ToString()
                }).ToList();

            // Apply number of customers filter (rooms must have capacity >= numberOfCustomers)
            if (numberOfCustomers.HasValue && numberOfCustomers.Value > 0)
            {
                rooms = rooms.Where(r => r.RoomMaxCapacity >= numberOfCustomers.Value).ToList();
            }

            // Apply max capacity filter (rooms must have exactly this capacity)
            if (maxCapacityFilter.HasValue && maxCapacityFilter.Value > 0)
            {
                rooms = rooms.Where(r => r.RoomMaxCapacity == maxCapacityFilter.Value).ToList();
            }

            // Apply address search filter if provided
            if (!string.IsNullOrEmpty(searchAddress))
            {
                rooms = rooms.Where(r => r.Address != null && r.Address.Contains(searchAddress, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply headline search filter if provided
            if (!string.IsNullOrEmpty(searchHeadline))
            {
                rooms = rooms.Where(r => r.RoomHeadline != null && r.RoomHeadline.Contains(searchHeadline, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Apply room type filter if provided
            if (roomTypeFilter.HasValue && roomTypeFilter.Value > 0)
            {
                rooms = rooms.Where(r => r.RoomTypeID == roomTypeFilter.Value).ToList();
            }

            // Filter rooms based on NumberOfRoomsAvailable > 0
            rooms = rooms.Where(r => r.NumberOfRoomsAvailable > 0).ToList();

            // Create a list of view models to hold room information and remaining available rooms
            var roomViewModels = new List<RoomInformationViewModel>();
            var unavailableRooms = new List<string>(); // To store names of unavailable rooms for notification

            // Apply date-based availability filter if check-in and check-out dates are provided
            if (checkIn.HasValue && checkOut.HasValue)
            {
                if (checkOut <= checkIn)
                {
                    ViewBag.ErrorMessage = "Check-out date must be after the check-in date.";
                    ViewBag.TotalPages = 0;
                    ViewBag.CurrentPage = 1;
                    return View(new List<RoomInformationViewModel>());
                }
                else if (checkIn < DateTime.Today)
                {
                    ViewBag.ErrorMessage = "Check-in date cannot be in the past.";
                    ViewBag.TotalPages = 0;
                    ViewBag.CurrentPage = 1;
                    return View(new List<RoomInformationViewModel>());
                }
                else if (checkOut < DateTime.Today)
                {
                    ViewBag.ErrorMessage = "Check-out date cannot be in the past.";
                    ViewBag.TotalPages = 0;
                    ViewBag.CurrentPage = 1;
                    return View(new List<RoomInformationViewModel>());
                }

                foreach (var room in rooms)
                {
                    // Calculate remaining available rooms using the repository
                    int remainingAvailableRooms = await _bookingRepository.GetRemainingAvailableRooms(room.RoomID, checkIn, checkOut);

                    // If no rooms are available, add to the unavailable list for notification
                    if (remainingAvailableRooms == 0)
                    {
                        unavailableRooms.Add(room.RoomHeadline);
                        continue; // Skip adding this room to the view model
                    }

                    roomViewModels.Add(new RoomInformationViewModel
                    {
                        Room = room,
                        RemainingAvailableRooms = remainingAvailableRooms
                    });
                }

                // Add notification for unavailable rooms
                if (unavailableRooms.Any())
                {
                    TempData["UnavailableRoomsMessage"] = $"The following rooms are fully booked for the selected dates: {string.Join(", ", unavailableRooms)}.";
                }
            }
            else
            {
                // If no dates are specified, show all rooms with their total NumberOfRoomsAvailable
                roomViewModels = rooms.Select(room => new RoomInformationViewModel
                {
                    Room = room,
                    RemainingAvailableRooms = room.NumberOfRoomsAvailable
                }).ToList();
            }

            // Pass filter values to the view for persistence
            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.SearchAddress = searchAddress;
            ViewBag.RoomTypeFilter = roomTypeFilter;
            ViewBag.SearchHeadline = searchHeadline;
            ViewBag.NumberOfCustomers = numberOfCustomers;
            ViewBag.MaxCapacityFilter = maxCapacityFilter;

            // Pagination logic
            var totalRooms = roomViewModels.Count();
            var paginatedRooms = roomViewModels.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalPages = (int)Math.Ceiling(totalRooms / (double)pageSize);
            ViewBag.CurrentPage = page;

            return View(paginatedRooms);
        }
        public async Task<IActionResult> Detail(int id)
        {
            var room = await _roomRepository.GetAllRoomIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }
            return View(room);
        }

        [Authorize(Roles = "Staff,Hotel Owner")]
        [HttpGet]
        public async Task<IActionResult> Manage(string searchHeadline, int? roomTypeFilter, string hotelOwnerFilter, int? page)
        {
            // Fetch all rooms and room types
            var rooms = await _roomRepository.GetAllRoomAsync();
            var roomTypes = await _roomRepository.GetAllRoomTypeAsync();

            // Populate ViewBag with room types for dropdowns
            ViewBag.RoomType = roomTypes.Select(rt => new SelectListItem
            {
                Value = rt.RoomTypeID.ToString(),
                Text = rt.RoomTypeName
            }).ToList();

            // Filter rooms for Hotel Owners
            if (User.IsInRole("Hotel Owner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                rooms = rooms.Where(r => r.AssignedHotelOwnerId == userId).ToList();
            }

            // Apply search and filter logic
            if (!string.IsNullOrEmpty(searchHeadline))
            {
                rooms = rooms.Where(r => r.RoomHeadline != null && r.RoomHeadline.Contains(searchHeadline, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (roomTypeFilter.HasValue && roomTypeFilter.Value > 0)
            {
                rooms = rooms.Where(r => r.RoomTypeID == roomTypeFilter.Value).ToList();
            }

            if (User.IsInRole("Staff") && !string.IsNullOrEmpty(hotelOwnerFilter))
            {
                rooms = rooms.Where(r => r.AssignedHotelOwnerId == hotelOwnerFilter).ToList();
            }

            // Set ViewBag flags for role-based UI rendering
            ViewBag.IsStaff = User.IsInRole("Staff");
            ViewBag.IsHotelOwner = User.IsInRole("Hotel Owner");

            // Prepare data for the Create form
            var hotelOwners = await _userManager.GetUsersInRoleAsync("Hotel Owner");
            ViewBag.HotelOwners = hotelOwners.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FirstName} {u.LastName} ({u.Email})"
            }).ToList();

            // Create a new RoomInformation object for the Create form
            var createRoomInfo = new RoomInformation();

            // Pre-select the first Hotel Owner for Staff (if any)
            if (User.IsInRole("Staff") && hotelOwners.Any())
            {
                createRoomInfo.AssignedHotelOwnerId = hotelOwners.First().Id;
                ViewBag.AssignedHotelOwnerName = $"{hotelOwners.First().FirstName} {hotelOwners.First().LastName} ({hotelOwners.First().Email})";
            }
            else if (User.IsInRole("Hotel Owner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                createRoomInfo.AssignedHotelOwnerId = userId;
                var currentUser = hotelOwners.FirstOrDefault(u => u.Id == userId);
                ViewBag.AssignedHotelOwnerName = currentUser != null
                    ? $"{currentUser.FirstName} {currentUser.LastName} ({currentUser.Email})"
                    : "No Hotel Owner";
            }
            else
            {
                ViewBag.AssignedHotelOwnerName = "No Hotel Owner";
            }

            // Pass the createRoomInfo to the ViewBag for the Create partial view
            ViewBag.CreateRoomInfo = createRoomInfo;

            // Pass filter values to the view to preserve them
            ViewBag.SearchHeadline = searchHeadline;
            ViewBag.RoomTypeFilter = roomTypeFilter;
            ViewBag.HotelOwnerFilter = hotelOwnerFilter;

            // Pagination logic
            int pageSize = 5; // Number of rooms per page
            int pageNumber = page ?? 1; // Default to page 1 if no page is specified

            // Calculate total rooms and paginate the data
            var totalRooms = rooms.Count();
            var paginatedRooms = rooms.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // Pass pagination data to the view
            ViewBag.TotalRooms = totalRooms;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRooms / (double)pageSize);

            return View(paginatedRooms);
        }

        [HttpPost]
        public async Task<IActionResult> CheckAvailability(int roomId, DateTime checkIn, DateTime checkOut)
        {
            // Check if at least 1 room is available
            bool isAvailable = await _bookingRepository.IsRoomAvailable(roomId, checkIn, checkOut, 1);

            if (!isAvailable)
            {
                int remainingRooms = await _bookingRepository.GetRemainingAvailableRooms(roomId, checkIn, checkOut);
                TempData["AvailabilityMessage"] = $"Room is NOT available for the selected dates. Only {remainingRooms} rooms are available.";
                TempData["AvailabilityStatus"] = "danger";
            }
            else
            {
                int remainingRooms = await _bookingRepository.GetRemainingAvailableRooms(roomId, checkIn, checkOut);
                TempData["AvailabilityMessage"] = $"Room is available! ({remainingRooms} rooms remaining)";
                TempData["AvailabilityStatus"] = "success";
            }

            return RedirectToAction("Detail", new { id = roomId, checkIn, checkOut });
        }

        [Authorize(Roles = "Staff")] // Only Staff can create rooms
        public async Task<IActionResult> Create()
        {
            var roomType = await _roomRepository.GetAllRoomTypeAsync();
            ViewBag.RoomType = roomType.Select(rt => new SelectListItem
            {
                Value = rt.RoomTypeID.ToString(),
                Text = rt.RoomTypeName
            }).ToList();

            // Get all Hotel Owners for the dropdown (visible only to Staff)
            var hotelOwners = await _userManager.GetUsersInRoleAsync("Hotel Owner");
            ViewBag.HotelOwners = hotelOwners.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FirstName} {u.LastName} ({u.Email})"
            }).ToList();

            // Set ViewBag.IsStaff
            ViewBag.IsStaff = User.IsInRole("Staff");

            // Create a new RoomInformation object
            var roomInfo = new RoomInformation();

            // If the user is a Hotel Owner, pre-set AssignedHotelOwnerId to their own ID (future-proofing)
            if (User.IsInRole("Hotel Owner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                roomInfo.AssignedHotelOwnerId = userId;

                // Set ViewBag.AssignedHotelOwnerName for the read-only case
                var currentUser = hotelOwners.FirstOrDefault(u => u.Id == userId);
                ViewBag.AssignedHotelOwnerName = currentUser != null
                    ? $"{currentUser.FirstName} {currentUser.LastName} ({currentUser.Email})"
                    : "No Hotel Owner";
            }
            else
            {
                // For Staff, optionally pre-select a default Hotel Owner (e.g., the first one in the list)
                if (hotelOwners.Any())
                {
                    roomInfo.AssignedHotelOwnerId = hotelOwners.First().Id;
                    ViewBag.AssignedHotelOwnerName = $"{hotelOwners.First().FirstName} {hotelOwners.First().LastName} ({hotelOwners.First().Email})";
                }
                else
                {
                    ViewBag.AssignedHotelOwnerName = "No Hotel Owner";
                }
            }

            return View(roomInfo);
        }
        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Create(RoomInformation roomInfo)
        {
            if (roomInfo == null)
            {
                return BadRequest("Invalid Room Information");
            }

            // If the user is a Hotel Owner, automatically set AssignedHotelOwnerId to their own ID (future-proofing)
            if (User.IsInRole("Hotel Owner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                roomInfo.AssignedHotelOwnerId = userId;
            }

            // Ensure an image is provided for new rooms
            if (roomInfo.ImageFile == null)
            {
                ModelState.AddModelError("ImageFile", "The Room Image field is required.");
            }

            if (ModelState.IsValid)
            {
                // Repopulate ViewBag for the form
                var roomType = await _roomRepository.GetAllRoomTypeAsync();
                ViewBag.RoomType = roomType.Select(rt => new SelectListItem
                {
                    Value = rt.RoomTypeID.ToString(),
                    Text = rt.RoomTypeName
                }).ToList();

                var hotelOwners = await _userManager.GetUsersInRoleAsync("Hotel Owner");
                ViewBag.HotelOwners = hotelOwners.Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                }).ToList();

                // Set ViewBag.IsStaff
                ViewBag.IsStaff = User.IsInRole("Staff");

                // Set ViewBag.AssignedHotelOwnerName for the read-only case
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var assignedHotelOwner = hotelOwners.FirstOrDefault(u => u.Id == (roomInfo.AssignedHotelOwnerId ?? userId));
                ViewBag.AssignedHotelOwnerName = assignedHotelOwner != null
                    ? $"{assignedHotelOwner.FirstName} {assignedHotelOwner.LastName} ({assignedHotelOwner.Email})"
                    : "No Hotel Owner";

                return View(roomInfo);
            }

            if (roomInfo.ImageFile != null)
            {
                roomInfo.RoomPicture = await _fileService.SaveFile(
                    roomInfo.ImageFile, "Images", new string[] { ".jpg", ".jpeg", ".png", ".webp" });
            }

            // RoomStatus is already set to 0 (inactive) by default in the model
            await _roomRepository.AddRoomAsync(roomInfo);
            return RedirectToAction("Manage");
        }
        [Authorize(Roles = "Staff,Hotel Owner")]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _roomRepository.GetAllRoomIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // Staff can edit any room
            if (User.IsInRole("Staff"))
            {
                // Staff can edit all rooms, including unassigned ones
            }
            else if (User.IsInRole("Hotel Owner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (room.AssignedHotelOwnerId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this room.";
                    return RedirectToAction("Manage");
                }

                // Hotel Owners cannot edit active rooms
                if (room.RoomStatus == 1)
                {
                    TempData["ErrorMessage"] = "Cannot edit an active room. Please ask Staff to set the room to inactive first.";
                    return RedirectToAction("Manage");
                }
            }

            var roomType = await _roomRepository.GetAllRoomTypeAsync();
            ViewBag.RoomType = roomType.Select(rt => new SelectListItem
            {
                Value = rt.RoomTypeID.ToString(),
                Text = rt.RoomTypeName
            }).ToList();

            // Get all Hotel Owners for the dropdown (visible only to Staff)
            var hotelOwners = await _userManager.GetUsersInRoleAsync("Hotel Owner");
            ViewBag.HotelOwners = hotelOwners.Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = $"{u.FirstName} {u.LastName} ({u.Email})"
            }).ToList();

            // Compute the assigned Hotel Owner's name for display
            var assignedHotelOwner = hotelOwners.FirstOrDefault(u => u.Id == room.AssignedHotelOwnerId);
            ViewBag.AssignedHotelOwnerName = assignedHotelOwner != null
                ? $"{assignedHotelOwner.FirstName} {assignedHotelOwner.LastName} ({assignedHotelOwner.Email})"
                : "No Hotel Owner";

            ViewBag.IsStaff = User.IsInRole("Staff");
            return View(room);
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Hotel Owner")]
        public async Task<IActionResult> Edit(int id, RoomInformation roomInfo)
        {
            if (id != roomInfo.RoomID)
            {
                return BadRequest();
            }

            var existingRoom = await _roomRepository.GetAllRoomIdAsync(id);
            if (existingRoom == null)
            {
                return NotFound();
            }

            // Staff can edit any room
            if (User.IsInRole("Staff"))
            {
                // Staff can edit all rooms, including unassigned ones
            }
            else if (User.IsInRole("Hotel Owner"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (existingRoom.AssignedHotelOwnerId != userId)
                {
                    TempData["ErrorMessage"] = "You are not authorized to edit this room.";
                    return RedirectToAction("Manage");
                }

                // Hotel Owners cannot edit active rooms
                if (existingRoom.RoomStatus == 1)
                {
                    TempData["ErrorMessage"] = "Cannot edit an active room. Please ask Staff to set the room to inactive first.";
                    return RedirectToAction("Manage");
                }

                // Hotel Owners cannot change the AssignedHotelOwnerId
                roomInfo.AssignedHotelOwnerId = existingRoom.AssignedHotelOwnerId;
            }

            // Preserve the existing RoomPicture if no new image is uploaded
            if (roomInfo.ImageFile == null)
            {
                roomInfo.RoomPicture = existingRoom.RoomPicture; // Ensure the old image is preserved
            }

            // If the room already has an image or a new image is uploaded, clear the validation error for ImageFile
            if (!string.IsNullOrEmpty(existingRoom.RoomPicture) || roomInfo.ImageFile != null)
            {
                ModelState.Remove("ImageFile");
            }
            else if (roomInfo.ImageFile == null && string.IsNullOrEmpty(existingRoom.RoomPicture))
            {
                // If there's no existing image and no new image is uploaded, add a validation error
                ModelState.AddModelError("ImageFile", "The Room Image field is required if no image exists.");
            }

            // Validate the model
            if (ModelState.IsValid)
            {
                // Repopulate ViewBag for the form
                var roomType = await _roomRepository.GetAllRoomTypeAsync();
                ViewBag.RoomType = roomType.Select(rt => new SelectListItem
                {
                    Value = rt.RoomTypeID.ToString(),
                    Text = rt.RoomTypeName
                }).ToList();

                var hotelOwners = await _userManager.GetUsersInRoleAsync("Hotel Owner");
                ViewBag.HotelOwners = hotelOwners.Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName} ({u.Email})"
                }).ToList();

                var assignedHotelOwner = hotelOwners.FirstOrDefault(u => u.Id == roomInfo.AssignedHotelOwnerId);
                ViewBag.AssignedHotelOwnerName = assignedHotelOwner != null
                    ? $"{assignedHotelOwner.FirstName} {assignedHotelOwner.LastName} ({assignedHotelOwner.Email})"
                    : "No Hotel Owner";

                ViewBag.IsStaff = User.IsInRole("Staff");
                return View(roomInfo);
            }

            // Update the existingRoom with values from roomInfo
            existingRoom.RoomMaxCapacity = roomInfo.RoomMaxCapacity;
            existingRoom.RoomHeadline = roomInfo.RoomHeadline;
            existingRoom.RoomTypeID = roomInfo.RoomTypeID;
            existingRoom.NumOfBed = roomInfo.NumOfBed;
            existingRoom.NumOfBath = roomInfo.NumOfBath;
            existingRoom.RoomPricePerDay = roomInfo.RoomPricePerDay;
            existingRoom.IsFeature = roomInfo.IsFeature;
            existingRoom.Address = roomInfo.Address;
            existingRoom.AssignedHotelOwnerId = roomInfo.AssignedHotelOwnerId; // Staff can update this
            existingRoom.RoomDetailDescription = roomInfo.RoomDetailDescription; // Update RoomDetailDescription
            existingRoom.NumberOfRoomsAvailable = roomInfo.NumberOfRoomsAvailable; 

            // Handle image upload
            string? oldRoomPicture = null;
            if (roomInfo.ImageFile != null)
            {
                oldRoomPicture = existingRoom.RoomPicture;
                existingRoom.RoomPicture = await _fileService.SaveFile(
                    roomInfo.ImageFile, "Images", new string[] { ".jpg", ".jpeg", ".png", ".webp" });
            }
            else
            {
                existingRoom.RoomPicture = roomInfo.RoomPicture; // Preserve the old image
            }

            // Preserve the RoomStatus from the existing room (Staff can change it via ToggleStatus)
            existingRoom.RoomStatus = existingRoom.RoomStatus;

            // Update the existingRoom (which is already tracked)
            await _roomRepository.UpdateRoomAsync(existingRoom);

            // Delete the old image if a new one was uploaded
            if (!string.IsNullOrWhiteSpace(oldRoomPicture))
            {
                _fileService.DeleteFile(oldRoomPicture, "Images");
            }

            return RedirectToAction("Manage");
        }
        [Authorize(Roles = "Staff")] // Only Staff can delete rooms
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _roomRepository.GetAllRoomIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _roomRepository.GetAllRoomIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            await _roomRepository.DeleteRoomAsync(id);
            if (!string.IsNullOrWhiteSpace(room.RoomPicture))
            {
                _fileService.DeleteFile(room.RoomPicture, "Images");
            }
            return RedirectToAction("Manage");
        }

        [HttpPost]
        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var room = await _roomRepository.GetAllRoomIdAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            // Toggle the RoomStatus (0 = inactive, 1 = active)
            room.RoomStatus = (byte)(room.RoomStatus == 0 ? 1 : 0);
            await _roomRepository.UpdateRoomAsync(room);

            TempData["StatusMessage"] = $"Room {room.RoomHeadline} is now {(room.RoomStatus == 1 ? "active" : "inactive")}.";
            return RedirectToAction("Manage");
        }

        [HttpPost]
        public async Task<IActionResult> BookNow(int roomId, DateTime checkIn, DateTime checkOut, int quantity = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var room = await _roomRepository.GetAllRoomIdAsync(roomId);
            if (room == null) return NotFound();

            // Check if the requested quantity is available
            bool isAvailable = await _bookingRepository.IsRoomAvailable(roomId, checkIn, checkOut, quantity);
            if (!isAvailable)
            {
                int remainingRooms = await _bookingRepository.GetRemainingAvailableRooms(roomId, checkIn, checkOut);
                TempData["ErrorMessage"] = $"This room is not available for the selected dates ({checkIn.ToString("yyyy-MM-dd")} to {checkOut.ToString("yyyy-MM-dd")}). Only {remainingRooms} rooms are available.";
                return RedirectToAction("Index", "Room", new { checkIn, checkOut });
            }

            // Create a new BookingReservation
            var bookingReservation = new BookingReservation
            {
                CustomerID = userId,
                BookingDate = DateTime.UtcNow,
                TotalPrice = 0,
                BookingStatus = 0 // Pending status
            };

            await _bookingRepository.AddReservationAsync(bookingReservation);

            // Create the BookingDetail
            var bookingDetail = new BookingDetail
            {
                RoomID = roomId,
                StartDate = checkIn,
                EndDate = checkOut,
                ActualPrice = room.RoomPricePerDay * (float)(checkOut - checkIn).TotalDays * quantity,
                Quantity = quantity,
                BookingReservationID = bookingReservation.BookingReservationID
            };

            await _bookingRepository.AddBookingDetailAsync(bookingDetail);

            // Update the total price of the reservation
            bookingReservation.TotalPrice = bookingDetail.ActualPrice;
            await _bookingRepository.UpdateReservationAsync(bookingReservation);

            TempData["SuccessMessage"] = "Room booked successfully!";
            return RedirectToAction("Reservation", "Booking");
        }

        public async Task<IActionResult> Statistics()
        {
            var currentDate = DateTime.Now;
            var rooms = await _roomRepository.GetAllRoomAsync();
            var totalRooms = rooms.Count();
            var reservations = await _bookingRepository.GetAllReservationAsync();
            var activeReservations = reservations.Where(r => r.BookingStatus == 1);

            var occupiedRooms = 0;
            foreach (var reservation in activeReservations)
            {
                var details = reservation.BookingDetails;
                if (details != null)
                {
                    foreach (var detail in details)
                    {
                        if (detail.StartDate <= currentDate && detail.EndDate >= currentDate)
                        {
                            occupiedRooms++;
                            break;
                        }
                    }
                }
            }

            var occupancyRate = totalRooms > 0 ? (decimal)occupiedRooms / totalRooms * 100 : 0;
            var totalRevenue = activeReservations
                .Sum(r => r.BookingDetails.Sum(d => d.ActualPrice ?? 0.0f));

            var model = new StatisticsViewModel
            {
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                OccupancyRate = decimal.Round(occupancyRate, 2),
                TotalRevenue = decimal.Round((decimal)totalRevenue, 2)
            };

            return View(model);
        }
    }
}