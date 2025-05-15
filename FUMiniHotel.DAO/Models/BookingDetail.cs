using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FUMiniHotel.DAO;

namespace FUMiniHotel.DAO
{
    public class BookingDetail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookingDetailID { get; set; }

        [Required]
        public int BookingReservationID { get; set; } // Explicit Foreign Key

        [ForeignKey("BookingReservationID")]
        public BookingReservation BookingReservation { get; set; } // Navigation Property
        public string? RoomNumber { get; set; }
        [Required]
        public int RoomID { get; set; } // Foreign Key for Room

        [ForeignKey("RoomID")]
        public RoomInformation Room { get; set; } // Navigation Property

        [Column(TypeName = "decimal(18,2)")]
        public float? ActualPrice { get; set; }

        [Required]
        public DateTime? StartDate { get; set; }
        public int Quantity { get; set; }
        [Required]
        public DateTime? EndDate { get; set; }
    }
}
