using System.Diagnostics;
using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.DAO;
using FUMiniHotel.DAO.ViewModel;
using FUMiniHotel.Repositories;
using FUMiniHotel.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FUMiniHotel.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRoomRepository _roomRepository;
        private readonly IBookingRepository _bookingRepository;

        public HomeController(
            ILogger<HomeController> logger,
            UserManager<ApplicationUser> userManager,
            IRoomRepository roomRepository,
            IBookingRepository bookingRepository)
        {
            _logger = logger;
            _userManager = userManager;
            _roomRepository = roomRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task<IActionResult> Index(
            DateTime? checkIn,
            DateTime? checkOut,
            string searchAddress = null,
            int? roomTypeFilter = null,
            string searchHeadline = null,
            int? numberOfCustomers = null,
            int? maxCapacityFilter = null,
            int pageNumber = 1)
        {
            ViewData["UserID"] = _userManager.GetUserId(this.User);

            // Get all rooms
            var rooms = await _roomRepository.GetAllRoomAsync();

            // Filter for featured rooms only
            rooms = rooms.Where(r => r.IsFeature).ToList();

            // Get room types for the filter dropdown
            var roomTypes = await _roomRepository.GetAllRoomTypeAsync();

            // Populate ViewBag.RoomType for the dropdowns
            ViewBag.RoomType = roomTypes.Select(rt => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = rt.RoomTypeID.ToString(),
                Text = rt.RoomTypeName
            }).ToList();

            // Populate ViewBag.MaxCapacities for the max capacity filter dropdown
            ViewBag.MaxCapacities = rooms
                .Select(r => r.RoomMaxCapacity)
                .Distinct()
                .OrderBy(cap => cap)
                .Select(cap => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
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

            // Create a list to hold the view models
            var roomViewModels = new List<RoomInformationViewModel>();
            var unavailableRooms = new List<string>(); // To store names of unavailable rooms for notification

            // If check-in and check-out dates are provided, filter available rooms
            if (checkIn.HasValue && checkOut.HasValue)
            {
                if (checkOut <= checkIn)
                {
                    ViewBag.ErrorMessage = "Check-out date must be after the check-in date.";
                    return View(new PagedResult<RoomInformationViewModel>
                    {
                        Items = new List<RoomInformationViewModel>(),
                        TotalItems = 0,
                        PageNumber = pageNumber,
                        PageSize = 3
                    });
                }
                else if (checkIn < DateTime.Today)
                {
                    ViewBag.ErrorMessage = "Check-in date cannot be in the past.";
                    return View(new PagedResult<RoomInformationViewModel>
                    {
                        Items = new List<RoomInformationViewModel>(),
                        TotalItems = 0,
                        PageNumber = pageNumber,
                        PageSize = 3
                    });
                }

                // Filter rooms that are available for the selected dates
                foreach (var room in rooms)
                {
                    int remainingRooms = await _bookingRepository.GetRemainingAvailableRooms(room.RoomID, checkIn, checkOut);
                    if (remainingRooms == 0)
                    {
                        unavailableRooms.Add(room.RoomHeadline);
                        continue; // Skip adding this room to the view model
                    }

                    roomViewModels.Add(new RoomInformationViewModel
                    {
                        Room = room,
                        RemainingAvailableRooms = remainingRooms
                    });
                }

                // Add notification for unavailable rooms
                if (unavailableRooms.Any())
                {
                    TempData["UnavailableRoomsMessage"] = $"The following featured rooms are fully booked for the selected dates: {string.Join(", ", unavailableRooms)}.";
                }
            }
            else
            {
                // If no dates are provided, show all featured rooms with their total NumberOfRoomsAvailable
                roomViewModels = rooms.Select(room => new RoomInformationViewModel
                {
                    Room = room,
                    RemainingAvailableRooms = room.NumberOfRoomsAvailable
                }).ToList();
            }

            // Pagination logic
            const int pageSize = 3;
            int totalItems = roomViewModels.Count;
            roomViewModels = roomViewModels
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var pagedResult = new PagedResult<RoomInformationViewModel>
            {
                Items = roomViewModels,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            // Pass filter values to the view for persistence
            ViewBag.CheckIn = checkIn;
            ViewBag.CheckOut = checkOut;
            ViewBag.SearchAddress = searchAddress;
            ViewBag.RoomTypeFilter = roomTypeFilter;
            ViewBag.SearchHeadline = searchHeadline;
            ViewBag.NumberOfCustomers = numberOfCustomers;
            ViewBag.MaxCapacityFilter = maxCapacityFilter;

            return View(pagedResult);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    // Helper class for pagination
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}