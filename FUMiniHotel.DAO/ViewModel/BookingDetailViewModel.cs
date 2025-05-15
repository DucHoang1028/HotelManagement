using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FUMiniHotel.DAO.ViewModel
{
    public class BookingDetailViewModel
    {
        [NotMapped]
        public IFormFile ImageFile { get; set; }
        public string RoomTypeName { get; set; }
        public string RoomPicture { get; set; }
        public string RoomHeadline { get; set; }
        public int BookingReservationID { get; set; }
        public string CustomerID { get; set; }
        public string CustomerName { get; set; }
        public DateTime? BookingDate { get; set; }
        public float? TotalPrice { get; set; }
        public byte BookingStatus { get; set; } // 0 = Pending, 1 = Confirmed, etc.
        public List<BookingDetail> BookingDetails { get; set; }
        // New Fields (Optional)
        public int RoomID { get; set; } // Adding RoomID for clarity in mapping
        public string RoomNumber { get; set; } // Useful for displaying room details
        public int RoomMaxCapacity { get; set; } // To display max capacity in UI
        [BindProperty] // Add this attribute
        public BookingDetail NewBookingDetail { get; set; } = new BookingDetail();
        public string PhoneNumber { get; set; } // Add this
        public string Email { get; set; } // Add this
    }


}
