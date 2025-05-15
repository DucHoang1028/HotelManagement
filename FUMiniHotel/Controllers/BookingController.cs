using System;
using System.Linq;
using System.Threading.Tasks;
using FUMiniHotel.DAO;
using FUMiniHotel.Repositories.IRepositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FUMiniHotel.Shared;
using Microsoft.AspNetCore.Mvc.Rendering;
using FUMiniHotel.DAO.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FUMiniHotel.Areas.Identity.Data;

namespace FUMiniHotel.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(
            IRoomRepository roomRepository,
            IBookingRepository bookingRepository,
            UserManager<ApplicationUser> userManager)
        {
            _roomRepository = roomRepository;
            _bookingRepository = bookingRepository;
            _userManager = userManager;
        }

        [HttpGet]
        [Authorize(Roles = "Hotel Owner,Staff")]
        public async Task<IActionResult> Manage(string searchQuery, string roleFilter, int? page, int? statusFilter, DateTime? bookingDate, string customerNameFilter)
        {
            // Get all reservations with navigation properties loaded
            var reservations = await _bookingRepository.GetAllReservationAsync() ?? new List<BookingReservation>();

            // Get the current user's ID
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized();

            // Apply filtering based on the user's role
            var filteredReservations = new List<BookingReservation>();
            if (User.IsInRole("Hotel Owner"))
            {
                filteredReservations = reservations
                    .Where(r => r.BookingStatus >= 2)
                    .Where(r => r.BookingDetails != null && r.BookingDetails.Any(bd =>
                        bd.Room != null &&
                        bd.Room.AssignedHotelOwnerId != null &&
                        bd.Room.AssignedHotelOwnerId == currentUserId))
                    .ToList();
            }
            else if (User.IsInRole("Staff"))
            {
                filteredReservations = reservations
                    .Where(r => r.BookingStatus >= 1)
                    .ToList();
            }

            // Apply status filter if provided
            if (statusFilter.HasValue && statusFilter.Value >= 1)
            {
                filteredReservations = filteredReservations
                    .Where(r => r.BookingStatus == statusFilter.Value)
                    .ToList();
            }

            // Apply booking date filter if provided
            if (bookingDate.HasValue)
            {
                filteredReservations = filteredReservations
                    .Where(r => r.BookingDate.HasValue && r.BookingDate.Value.Date == bookingDate.Value.Date)
                    .ToList();
            }

            // Fetch customer names for each reservation and apply customer name filter
            var reservationsWithCustomerNames = new List<(BookingReservation Reservation, string CustomerName)>();
            foreach (var reservation in filteredReservations)
            {
                var customer = await _userManager.FindByIdAsync(reservation.CustomerID);
                string customerName = customer != null
                    ? $"{customer.FirstName} {customer.LastName}"
                    : "Unknown Customer";
                reservationsWithCustomerNames.Add((reservation, customerName));
            }

            // Apply customer name filter if provided
            if (!string.IsNullOrEmpty(customerNameFilter))
            {
                reservationsWithCustomerNames = reservationsWithCustomerNames
                    .Where(r => r.CustomerName.Contains(customerNameFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                filteredReservations = reservationsWithCustomerNames.Select(r => r.Reservation).ToList();
            }

            // Populate ViewBag.CustomerNames for the dropdown
            var allCustomers = await _userManager.Users.ToListAsync();
            ViewBag.CustomerNames = allCustomers
                .Select(c => new SelectListItem
                {
                    Value = $"{c.FirstName} {c.LastName}",
                    Text = $"{c.FirstName} {c.LastName}"
                })
                .Distinct()
                .OrderBy(c => c.Text)
                .ToList();

            // Add an "All Customers" option
            ViewBag.CustomerNames.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "All Customers"
            });

            // Populate ViewBag.Statuses for the status dropdown
            ViewBag.Statuses = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "All Statuses" },
        new SelectListItem { Value = "1", Text = "Need Confirm" },
        new SelectListItem { Value = "2", Text = "Confirmed" },
        new SelectListItem { Value = "3", Text = "Active" },
        new SelectListItem { Value = "4", Text = "Finished" },
        new SelectListItem { Value = "5", Text = "Canceled" }
    };

            // Pagination logic
            int pageSize = 5;
            int pageNumber = page ?? 1;

            var totalReservations = filteredReservations.Count();
            var paginatedReservations = filteredReservations.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            // Pass filter values to the view for persistence
            ViewBag.CurrentUserId = currentUserId;
            ViewBag.IsHotelOwner = User.IsInRole("Hotel Owner");
            ViewBag.ReservationsWithCustomerNames = reservationsWithCustomerNames;
            ViewBag.TotalReservations = totalReservations;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalReservations / (double)pageSize);
            ViewBag.StatusFilter = statusFilter;
            ViewBag.BookingDate = bookingDate;
            ViewBag.CustomerNameFilter = customerNameFilter;

            return View(paginatedReservations);
        }

        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Unauthorized();

            var reservation = await _bookingRepository.GetAllReservationIdAsync(id);
            if (reservation == null)
            {
                return NotFound("Booking reservation not found.");
            }

            bool canViewDetails = false;
            if (reservation.BookingDetails != null && reservation.BookingDetails.Any())
            {
                foreach (var detail in reservation.BookingDetails)
                {
                    var room = await _roomRepository.GetAllRoomIdAsync(detail.RoomID);
                    if (room != null && room.AssignedHotelOwnerId == currentUserId)
                    {
                        canViewDetails = true;
                        break;
                    }
                }
            }

            if (!canViewDetails && !User.IsInRole("Staff"))
            {
                return Forbid("You do not have permission to view this booking.");
            }

            var customer = await _userManager.FindByIdAsync(reservation.CustomerID);
            string customerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown Customer";
            string phoneNumber = customer?.PhoneNumber ?? "N/A";
            string email = customer?.Email ?? "N/A";

            var bookingDetailViewModel = new BookingDetailViewModel
            {
                BookingReservationID = reservation.BookingReservationID,
                CustomerID = reservation.CustomerID,
                CustomerName = customerName,
                PhoneNumber = phoneNumber,
                Email = email,
                BookingDate = reservation.BookingDate,
                TotalPrice = reservation.TotalPrice,
                BookingStatus = reservation.BookingStatus,
                BookingDetails = reservation.BookingDetails.ToList()
            };

            return View(bookingDetailViewModel);
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
                Quantity = quantity, // Set the quantity
                BookingReservationID = bookingReservation.BookingReservationID
            };

            await _bookingRepository.AddBookingDetailAsync(bookingDetail);

            // Update the total price of the reservation
            bookingReservation.TotalPrice = bookingDetail.ActualPrice;
            await _bookingRepository.UpdateReservationAsync(bookingReservation);

            TempData["SuccessMessage"] = "Room booked successfully!";
            return RedirectToAction("Reservation", "Booking");
        }
        public async Task<IActionResult> Reservation()
        {
            var room = await _roomRepository.GetAllRoomAsync();
            ViewBag.RoomPictures = room.ToDictionary(r => r.RoomID, r => r.RoomPicture);
            ViewBag.Room = room.Select(rt => new SelectListItem
            {
                Value = rt.RoomID.ToString(),
                Text = rt.RoomHeadline,
            }).ToList();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var userCart = await _bookingRepository.GetAllBookingDetailAsync();
            userCart = userCart.Where(b => b.BookingReservation == null || b.BookingReservation.BookingStatus == 0).ToList();

            // Check for rooms that have run out (NumberOfRoomsAvailable == 0)
            var itemsToRemove = new List<int>();
            foreach (var item in userCart)
            {
                var roomInfo = await _roomRepository.GetAllRoomIdAsync(item.RoomID);
                if (roomInfo != null && roomInfo.NumberOfRoomsAvailable == 0)
                {
                    itemsToRemove.Add(item.BookingDetailID);
                    TempData["ErrorMessage"] = $"Room {roomInfo.RoomHeadline} has run out and has been removed from your reservation.";
                }
            }

            // Remove items that have run out
            foreach (var bookingDetailId in itemsToRemove)
            {
                await _bookingRepository.DeleteBookingDetailAsync(bookingDetailId);
            }

            // Refresh the cart after removals
            userCart = await _bookingRepository.GetAllBookingDetailAsync();
            userCart = userCart.Where(b => b.BookingReservation == null || b.BookingReservation.BookingStatus == 0).ToList();

            return View(userCart);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int bookingDetailId, int quantity)
        {
            var bookingDetail = await _bookingRepository.GetAllBookingDetailIdAsync(bookingDetailId);
            if (bookingDetail == null)
            {
                TempData["ErrorMessage"] = "Booking detail not found.";
                return RedirectToAction("Reservation");
            }

            var room = await _roomRepository.GetAllRoomIdAsync(bookingDetail.RoomID);
            if (room == null)
            {
                TempData["ErrorMessage"] = "Room not found.";
                return RedirectToAction("Reservation");
            }

            // Validate quantity
            if (quantity <= 0)
            {
                TempData["ErrorMessage"] = "Quantity must be greater than 0.";
                return RedirectToAction("Reservation");
            }

            if (quantity > room.NumberOfRoomsAvailable)
            {
                TempData["ErrorMessage"] = $"Room {room.RoomHeadline} only has {room.NumberOfRoomsAvailable} rooms available.";
                return RedirectToAction("Reservation");
            }

            // Update quantity and recalculate price
            bookingDetail.Quantity = quantity;
            var duration = (bookingDetail.EndDate.Value - bookingDetail.StartDate.Value).TotalDays;
            bookingDetail.ActualPrice = room.RoomPricePerDay * (float)duration * quantity;
            await _bookingRepository.UpdateBookingDetailAsync(bookingDetail);

            // Update the total price of the reservation
            var reservation = await _bookingRepository.GetAllReservationIdAsync(bookingDetail.BookingReservationID);
            if (reservation != null)
            {
                reservation.TotalPrice = reservation.BookingDetails.Sum(bd => bd.ActualPrice ?? 0);
                await _bookingRepository.UpdateReservationAsync(reservation);
            }

            return RedirectToAction("Reservation");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBooking()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var reservation = await _bookingRepository.GetActiveBookingReservation(userId);
            if (reservation == null)
            {
                TempData["ErrorMessage"] = "No active reservation found to confirm.";
                return RedirectToAction("Reservation");
            }

            // Validate quantities before confirming
            foreach (var detail in reservation.BookingDetails)
            {
                var room = await _roomRepository.GetAllRoomIdAsync(detail.RoomID);
                if (room == null)
                {
                    TempData["ErrorMessage"] = "One or more rooms in your reservation are not found.";
                    return RedirectToAction("Reservation");
                }

                if (room.NumberOfRoomsAvailable < detail.Quantity)
                {
                    TempData["ErrorMessage"] = $"Room {room.RoomHeadline} only has {room.NumberOfRoomsAvailable} rooms available.";
                    return RedirectToAction("Reservation");
                }

                if (room.NumberOfRoomsAvailable == 0)
                {
                    await _bookingRepository.DeleteBookingDetailAsync(detail.BookingDetailID);
                    TempData["ErrorMessage"] = $"Room {room.RoomHeadline} has run out and has been removed from your reservation.";
                    return RedirectToAction("Reservation");
                }

                // Update room availability (decrease NumberOfRoomsAvailable)
                room.NumberOfRoomsAvailable -= detail.Quantity;
                await _roomRepository.UpdateRoomAsync(room);
            }

            reservation.BookingStatus = 1; // Set status to Confirmed
            await _bookingRepository.UpdateReservationAsync(reservation);

            TempData["SuccessMessage"] = "Your reservation is successful! The staff will contact you shortly to confirm the order.";

            return RedirectToAction("Reservation");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFromCart(int bookingId)
        {
            await _bookingRepository.DeleteBookingDetailAsync(bookingId);
            return RedirectToAction("Reservation");
        }

        // Other actions remain unchanged for this requirement
        public async Task<IActionResult> Edit(int id)
        {
            var bookingReservation = await _bookingRepository.GetAllReservationIdAsync(id);
            if (bookingReservation == null) return NotFound("Booking reservation not found.");

            var customer = await _userManager.FindByIdAsync(bookingReservation.CustomerID);
            string customerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown Customer";
            string phoneNumber = customer?.PhoneNumber ?? "N/A";
            string email = customer?.Email ?? "N/A";

            var rooms = await _roomRepository.GetAllRoomAsync();
            ViewBag.Rooms = rooms.Select(r => new
            {
                Value = r.RoomID.ToString(),
                Text = r.RoomHeadline,
                RoomPricePerDay = r.RoomPricePerDay
            }).ToList();

            var viewModel = new BookingDetailViewModel
            {
                BookingReservationID = bookingReservation.BookingReservationID,
                CustomerID = bookingReservation.CustomerID,
                CustomerName = customerName,
                PhoneNumber = phoneNumber,
                Email = email,
                BookingDate = bookingReservation.BookingDate,
                TotalPrice = bookingReservation.TotalPrice,
                BookingStatus = bookingReservation.BookingStatus,
                BookingDetails = bookingReservation.BookingDetails.ToList(),
                NewBookingDetail = new BookingDetail()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int bookingReservationId, BookingDetailViewModel viewModel)
        {
            var bookingReservation = await _bookingRepository.GetAllReservationIdAsync(bookingReservationId);
            if (bookingReservation == null) return NotFound("Booking reservation not found.");

            foreach (var detail in viewModel.BookingDetails)
            {
                var bookingDetail = await _bookingRepository.GetAllBookingDetailIdAsync(detail.BookingDetailID);
                if (bookingDetail == null) continue;

                if (!detail.StartDate.HasValue || !detail.EndDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Start date and end date must be provided.";
                    return RedirectToAction("Edit", new { id = bookingReservationId });
                }

                DateTime today = DateTime.UtcNow.Date;
                if (detail.StartDate.Value < today || detail.EndDate.Value < today)
                {
                    TempData["ErrorMessage"] = "Start date and end date cannot be in the past.";
                    return RedirectToAction("Edit", new { id = bookingReservationId });
                }

                if (detail.StartDate.Value >= detail.EndDate.Value)
                {
                    TempData["ErrorMessage"] = "Start date must be before end date.";
                    return RedirectToAction("Edit", new { id = bookingReservationId });
                }

                var existingBookings = await _bookingRepository.GetRoomBookings(detail.RoomID, detail.StartDate.Value, detail.EndDate.Value);
                var conflictingBooking = existingBookings.FirstOrDefault(b => b.BookingDetailID != detail.BookingDetailID);
                if (conflictingBooking != null)
                {
                    TempData["ErrorMessage"] = $"Room {detail.RoomID} is already booked from {conflictingBooking.StartDate:yyyy-MM-dd} to {conflictingBooking.EndDate:yyyy-MM-dd} (Reservation ID: {conflictingBooking.BookingReservationID}).";
                    return RedirectToAction("Edit", new { id = bookingReservationId });
                }

                bookingDetail.RoomID = detail.RoomID;
                bookingDetail.RoomNumber = detail.RoomNumber;
                bookingDetail.StartDate = detail.StartDate.Value;
                bookingDetail.EndDate = detail.EndDate.Value;

                TimeSpan duration = detail.EndDate.Value - detail.StartDate.Value;
                var room = await _roomRepository.GetAllRoomIdAsync(detail.RoomID);
                if (room != null)
                {
                    bookingDetail.ActualPrice = room.RoomPricePerDay * (float)duration.TotalDays * (detail.Quantity > 0 ? detail.Quantity : 1);
                }

                await _bookingRepository.UpdateBookingDetailAsync(bookingDetail);
            }

            if (viewModel.NewBookingDetail != null && viewModel.NewBookingDetail.RoomID > 0)
            {
                var newDetail = new BookingDetail
                {
                    RoomID = viewModel.NewBookingDetail.RoomID,
                    RoomNumber = viewModel.NewBookingDetail.RoomNumber,
                    StartDate = viewModel.NewBookingDetail.StartDate ?? DateTime.UtcNow,
                    EndDate = viewModel.NewBookingDetail.EndDate ?? DateTime.UtcNow.AddDays(1),
                    ActualPrice = viewModel.NewBookingDetail.ActualPrice ?? 0,
                    Quantity = viewModel.NewBookingDetail.Quantity > 0 ? viewModel.NewBookingDetail.Quantity : 1,
                    BookingReservationID = bookingReservationId
                };

                if (!newDetail.StartDate.HasValue || !newDetail.EndDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Start and end dates are required.";
                    return RedirectToAction("Edit", new { id = bookingReservationId });
                }

                var room = await _roomRepository.GetAllRoomIdAsync(newDetail.RoomID);
                if (room != null)
                {
                    if (newDetail.Quantity > room.NumberOfRoomsAvailable)
                    {
                        TempData["ErrorMessage"] = $"Room {room.RoomHeadline} only has {room.NumberOfRoomsAvailable} rooms available.";
                        return RedirectToAction("Edit", new { id = bookingReservationId });
                    }

                    TimeSpan duration = newDetail.EndDate.Value - newDetail.StartDate.Value;
                    newDetail.ActualPrice = room.RoomPricePerDay * (float)duration.TotalDays * newDetail.Quantity;
                }

                await _bookingRepository.AddBookingDetailAsync(newDetail);
            }

            bookingReservation.TotalPrice = bookingReservation.BookingDetails.Sum(bd => bd.ActualPrice ?? 0);
            await _bookingRepository.UpdateReservationAsync(bookingReservation);

            TempData["SuccessMessage"] = "Booking details updated successfully!";
            return RedirectToAction("Edit", new { id = bookingReservationId });
        }

        [HttpPost]
        public async Task<IActionResult> AddReservation(BookingReservation model)
        {
            if (model == null) return BadRequest("Invalid reservation data.");

            await _bookingRepository.AddReservationAsync(model);
            return RedirectToAction("Manage");
        }

        [HttpPost]
        public async Task<IActionResult> AddBookingDetail(int reservationId, int roomId, DateTime startDate, DateTime endDate)
        {
            var room = await _roomRepository.GetAllRoomIdAsync(roomId);
            if (room == null) return NotFound("Room not found.");

            if (room.NumberOfRoomsAvailable <= 0)
            {
                TempData["ErrorMessage"] = $"Room {room.RoomHeadline} has run out.";
                return RedirectToAction("Edit", new { id = reservationId });
            }

            var bookingDetail = new BookingDetail
            {
                RoomID = roomId,
                StartDate = startDate,
                EndDate = endDate,
                Quantity = 1,
                ActualPrice = room.RoomPricePerDay * (float)(endDate - startDate).TotalDays * 1,
                BookingReservationID = reservationId
            };

            await _bookingRepository.AddBookingDetailAsync(bookingDetail);

            return RedirectToAction("Edit", new { id = reservationId });
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var rooms = await _roomRepository.GetAllRoomAsync();
            ViewBag.Rooms = rooms.Select(r => new
            {
                Value = r.RoomID.ToString(),
                Text = r.RoomHeadline,
                RoomPricePerDay = r.RoomPricePerDay
            }).ToList();

            var customers = await _userManager.Users.ToListAsync();
            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.Id,
                Text = $"{c.FirstName} {c.LastName} - {c.PhoneNumber}"
            }).ToList();

            var viewModel = new BookingDetailViewModel
            {
                BookingDate = DateTime.UtcNow,
                BookingStatus = 0,
                BookingDetails = new List<BookingDetail>(),
                NewBookingDetail = new BookingDetail()
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Create(BookingDetailViewModel viewModel)
        {
            // Populate ViewBag for rooms and customers (needed for both success and error cases)
            var rooms = await _roomRepository.GetAllRoomAsync();
            ViewBag.Rooms = rooms.Select(r => new
            {
                Value = r.RoomID.ToString(),
                Text = r.RoomHeadline,
                RoomPricePerDay = r.RoomPricePerDay
            }).ToList();

            var customers = await _userManager.Users.ToListAsync();
            ViewBag.Customers = customers.Select(c => new SelectListItem
            {
                Value = c.Id,
                Text = $"{c.FirstName} {c.LastName}"
            }).ToList();

            // Check if the model state is valid
            if (!ModelState.IsValid)
            {
                return View(viewModel); // Return the view with validation errors
            }

            // Create a new BookingReservation
            var bookingReservation = new BookingReservation
            {
                CustomerID = viewModel.CustomerID,
                BookingDate = DateTime.UtcNow,
                TotalPrice = 0,
                BookingStatus = viewModel.BookingStatus
            };

            await _bookingRepository.AddReservationAsync(bookingReservation);

            // Check if a new booking detail is provided
            if (viewModel.NewBookingDetail != null && viewModel.NewBookingDetail.RoomID > 0)
            {
                var newDetail = new BookingDetail
                {
                    RoomID = viewModel.NewBookingDetail.RoomID,
                    RoomNumber = viewModel.NewBookingDetail.RoomNumber,
                    StartDate = viewModel.NewBookingDetail.StartDate ?? DateTime.UtcNow,
                    EndDate = viewModel.NewBookingDetail.EndDate ?? DateTime.UtcNow.AddDays(1),
                    Quantity = viewModel.NewBookingDetail.Quantity > 0 ? viewModel.NewBookingDetail.Quantity : 1,
                    BookingReservationID = bookingReservation.BookingReservationID
                };

                // Validate dates
                if (!newDetail.StartDate.HasValue || !newDetail.EndDate.HasValue)
                {
                    TempData["ErrorMessage"] = "Start and end dates are required.";
                    return View(viewModel);
                }

                DateTime today = DateTime.UtcNow.Date;
                if (newDetail.StartDate.Value < today || newDetail.EndDate.Value < today)
                {
                    TempData["ErrorMessage"] = "Start date and end date cannot be in the past.";
                    return View(viewModel);
                }

                if (newDetail.StartDate.Value >= newDetail.EndDate.Value)
                {
                    TempData["ErrorMessage"] = "Start date must be before end date.";
                    return View(viewModel);
                }

                // Check room availability for the requested quantity
                bool isAvailable = await _bookingRepository.IsRoomAvailable(newDetail.RoomID, newDetail.StartDate, newDetail.EndDate, newDetail.Quantity);
                if (!isAvailable)
                {
                    var remainingRooms = await _bookingRepository.GetRemainingAvailableRooms(newDetail.RoomID, newDetail.StartDate, newDetail.EndDate);
                    TempData["ErrorMessage"] = $"This room is not available for the selected dates. Only {remainingRooms} rooms are available.";
                    return View(viewModel);
                }

                // Calculate the price based on the room and duration
                var room = await _roomRepository.GetAllRoomIdAsync(newDetail.RoomID);
                if (room != null)
                {
                    if (newDetail.Quantity > room.NumberOfRoomsAvailable)
                    {
                        TempData["ErrorMessage"] = $"Room {room.RoomHeadline} only has {room.NumberOfRoomsAvailable} rooms available.";
                        return View(viewModel);
                    }

                    TimeSpan duration = newDetail.EndDate.Value - newDetail.StartDate.Value;
                    newDetail.ActualPrice = room.RoomPricePerDay * (float)duration.TotalDays * newDetail.Quantity;
                }

                // Add the booking detail
                await _bookingRepository.AddBookingDetailAsync(newDetail);

                // Update the total price of the reservation
                bookingReservation.TotalPrice = newDetail.ActualPrice ?? 0;
                await _bookingRepository.UpdateReservationAsync(bookingReservation);
            }

            TempData["SuccessMessage"] = "Booking created successfully!";
            return RedirectToAction("Manage");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBookingDetail(int id, int bookingReservationId)
        {
            await _bookingRepository.DeleteBookingDetailAsync(id);
            return RedirectToAction("Edit", new { id = bookingReservationId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var bookingReservation = await _bookingRepository.GetAllReservationIdAsync(id);
            if (bookingReservation == null)
            {
                TempData["ErrorMessage"] = "Booking reservation not found.";
                return RedirectToAction("Manage");
            }

            var bookingDetails = bookingReservation.BookingDetails?.ToList() ?? new List<BookingDetail>();
            foreach (var detail in bookingDetails)
            {
                await _bookingRepository.DeleteBookingDetailAsync(detail.BookingDetailID);
            }

            try
            {
                await _bookingRepository.DeleteReservationAsync(id);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete booking reservation.";
                return RedirectToAction("Manage");
            }

            TempData["SuccessMessage"] = "Booking reservation deleted successfully!";
            return RedirectToAction("Manage");
        }
    }
}