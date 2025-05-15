using System.Collections.Generic;

namespace FUMiniHotel.Models
{
    public class QnAViewModel
    {
        public List<Question> GuestQuestions { get; set; }
        public List<Question> HotelOwnerQuestions { get; set; }
    }
}