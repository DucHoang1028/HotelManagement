using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FUMiniHotel.Areas.Identity.Data;

namespace FUMiniHotel.DAO
{
    public class BookingReservation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BookingReservationID { get; set; }

        public DateTime? BookingDate { get; set; }

        public float? TotalPrice { get; set; }

        [Required]
        public string CustomerID { get; set; }

        [ForeignKey("CustomerID")]
        public ApplicationUser Customer { get; set; }

        [Required]
        public byte BookingStatus { get; set; }

        public ICollection<BookingDetail> BookingDetails { get; set; }
    }
}
