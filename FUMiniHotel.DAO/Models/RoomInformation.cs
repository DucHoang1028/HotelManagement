using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FUMiniHotel.DAO;
using Microsoft.AspNetCore.Http;

namespace FUMiniHotel.DAO
{
    public class RoomInformation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public int RoomID { get; set; }

        public string? RoomHeadline { get; set; } // Make nullable

        public string? RoomDetailDescription { get; set; } // Make nullable

        public string? RoomPicture { get; set; } // Make nullable
        [NotMapped]
        public IFormFile ImageFile { get; set; }
        public int RoomMaxCapacity { get; set; }

        [Required]
        public int RoomTypeID { get; set; } 

        [ForeignKey("RoomTypeID")]
        public RoomType RoomType { get; set; }

        public byte RoomStatus { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public float RoomPricePerDay { get; set; }
        public int NumOfBed { get; set; }
        public int NumOfBath { get; set; }
        public bool IsFeature { get; set; }
        public string? Address { get; set; }
        public int NumberOfRoomsAvailable { get; set; }
        public string? AssignedHotelOwnerId { get; set; }
    }
}
