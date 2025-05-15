using FUMiniHotel.DAO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FUMiniHotel.Repositories.IRepositories
{
    public interface IBookingRepository
    {
        Task<IEnumerable<BookingReservation>> GetAllReservationAsync();
        Task<BookingReservation> GetAllReservationIdAsync(int id);
        Task AddReservationAsync(BookingReservation reservation);
        Task UpdateReservationAsync(BookingReservation reservation);
        Task DeleteReservationAsync(int id);

        Task<IEnumerable<BookingDetail>> GetAllBookingDetailAsync();
        Task<BookingDetail> GetAllBookingDetailIdAsync(int id);
        Task AddBookingDetailAsync(BookingDetail detail);
        Task UpdateBookingDetailAsync(BookingDetail detail);
        Task DeleteBookingDetailAsync(int id);

        Task<bool> IsRoomAvailable(int roomId, DateTime? checkIn, DateTime? checkOut, int quantity); // Updated to DateTime?
        Task<int> GetRemainingAvailableRooms(int roomId, DateTime? checkIn, DateTime? checkOut); // Updated to DateTime?
        Task ConfirmBookingAsync(string userId);
        Task<BookingReservation> GetActiveBookingReservation(string userId);
        Task<List<BookingDetail>> GetRoomBookings(int roomId, DateTime startDate, DateTime endDate);
    }
}