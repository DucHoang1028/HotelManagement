using FUMiniHotel.Areas.Identity.Data;

namespace FUMiniHotel.Models
{
    public enum Audience
    {
        Guest = 0,
        HotelOwner = 1,
        Both = 2
    }

    public class Question
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public string? AnswerText { get; set; }
        public string? AskedById { get; set; } // Nullable
        public string? AnsweredById { get; set; } // Nullable
        public DateTime AskedDate { get; set; }
        public DateTime? AnsweredDate { get; set; }
        public bool IsAnswered { get; set; }
        public bool IsFeatured { get; set; }
        public Audience Audience { get; set; }

        public ApplicationUser AskedBy { get; set; }
        public ApplicationUser AnsweredBy { get; set; }
    }
}