using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FUMiniHotel.DAO;
using FUMiniHotel.DAO.Data;
using FUMiniHotel.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace FUMiniHotel.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly FUMiniHotelContext _context;

        public BookingRepository(FUMiniHotelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookingReservation>> GetAllReservationAsync()
        {
            return await _context.BookingReservations
                .Include(b => b.BookingDetails)
                .Include(b => b.Customer)
                .ToListAsync();
        }

        public async Task<BookingReservation> GetAllReservationIdAsync(int id)
        {
            return await _context.BookingReservations
                           .Include(b => b.BookingDetails)
                           .Include(b => b.Customer)
                           .FirstOrDefaultAsync(b => b.BookingReservationID == id);
        }

        public async Task UpdateReservationAsync(BookingReservation reservation)
        {
            _context.BookingReservations.Update(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task AddReservationAsync(BookingReservation reservation)
        {
            await _context.BookingReservations.AddAsync(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteReservationAsync(int id)
        {
            var bookingRes = await _context.BookingReservations.FindAsync(id);
            if (bookingRes != null)
            {
                _context.BookingReservations.Remove(bookingRes);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<BookingDetail>> GetAllBookingDetailAsync()
        {
            return await _context.BookingDetail // Fixed: Changed BookingDetail to BookingDetails
                           .Include(d => d.BookingReservation)
                           .Include(d => d.Room)
                           .ToListAsync();
        }

        public async Task<BookingDetail> GetAllBookingDetailIdAsync(int id)
        {
            return await _context.BookingDetail // Fixed: Changed BookingDetail to BookingDetails
                           .Include(d => d.BookingReservation)
                           .Include(d => d.Room)
                           .FirstOrDefaultAsync(d => d.BookingDetailID == id);
        }

        public async Task AddBookingDetailAsync(BookingDetail detail)
        {
            await _context.BookingDetail.AddAsync(detail); // Fixed: Changed BookingDetail to BookingDetails
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBookingDetailAsync(BookingDetail detail)
        {
            _context.BookingDetail.Update(detail); // Fixed: Changed BookingDetail to BookingDetails
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBookingDetailAsync(int id)
        {
            var detail = await _context.BookingDetail.FindAsync(id); // Fixed: Changed BookingDetail to BookingDetails
            if (detail != null)
            {
                _context.BookingDetail.Remove(detail);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> GetRemainingAvailableRooms(int roomId, DateTime? checkIn, DateTime? checkOut)
        {
            // Validate dates
            if (!checkIn.HasValue || !checkOut.HasValue)
            {
                return 0; // Cannot calculate availability without dates
            }

            // Get the total number of rooms available for this room type
            var room = await _context.RoomInformation
                .FirstOrDefaultAsync(r => r.RoomID == roomId);
            if (room == null)
            {
                return 0; // Room not found
            }
            int totalRooms = room.NumberOfRoomsAvailable;

            // Get all bookings for this room that overlap with the requested dates
            var overlappingBookings = await _context.BookingDetail
                .Where(b => b.RoomID == roomId &&
                            b.StartDate < checkOut.Value && b.EndDate > checkIn.Value)
                .ToListAsync();

            if (!overlappingBookings.Any())
            {
                return totalRooms; // No overlapping bookings, all rooms are available
            }

            // Create a list of all days in the requested date range
            var dateRange = Enumerable.Range(0, (checkOut.Value - checkIn.Value).Days)
                .Select(d => checkIn.Value.AddDays(d))
                .ToList();

            // Calculate the maximum number of rooms booked on any day
            int maxRoomsBooked = 0;
            foreach (var day in dateRange)
            {
                int roomsBookedOnDay = overlappingBookings
                    .Where(b => b.StartDate <= day && b.EndDate > day)
                    .Sum(b => b.Quantity);
                maxRoomsBooked = Math.Max(maxRoomsBooked, roomsBookedOnDay);
            }

            // Calculate remaining available rooms
            int remainingRooms = totalRooms - maxRoomsBooked;
            return remainingRooms < 0 ? 0 : remainingRooms;
        }

        public async Task<bool> IsRoomAvailable(int roomId, DateTime? checkIn, DateTime? checkOut, int quantity)
        {
            if (!checkIn.HasValue || !checkOut.HasValue)
            {
                return false; // Cannot check availability without dates
            }

            int remainingRooms = await GetRemainingAvailableRooms(roomId, checkIn, checkOut);
            return remainingRooms >= quantity;
        }

        public async Task ConfirmBookingAsync(string userId)
        {
            var reservation = await _context.BookingReservations
                .Where(r => r.CustomerID == userId && r.BookingStatus == 0)
                .Include(r => r.BookingDetails)
                .FirstOrDefaultAsync();

            if (reservation != null)
            {
                reservation.BookingStatus = 1;
                _context.BookingReservations.Update(reservation);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<BookingReservation> GetActiveBookingReservation(string userId)
        {
            return await _context.BookingReservations
                .Include(r => r.BookingDetails)
                .FirstOrDefaultAsync(r => r.CustomerID == userId && r.BookingStatus == 0);
        }

        public async Task<List<BookingDetail>> GetRoomBookings(int roomId, DateTime startDate, DateTime endDate)
        {
            return await _context.BookingDetail
                .Where(bd => bd.RoomID == roomId &&
                             bd.StartDate < endDate && // Overlaps with the new booking
                             bd.EndDate > startDate)
                .ToListAsync();
        }
    }
}