using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.DAO.Data;
using FUMiniHotel.Models;
using FUMiniHotel.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FUMiniHotel.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly FUMiniHotelContext _context;

        public QuestionRepository(FUMiniHotelContext context)
        {
            _context = context;
        }

        public async Task AddQuestionAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Question>> GetAllQuestionsAsync()
        {
            return await _context.Questions
                .Include(q => q.AskedBy)
                .OrderByDescending(q => q.AskedDate)
                .ToListAsync();
        }

        public async Task<List<Question>> GetFeaturedQuestionsForAudienceAsync(Audience audience)
        {
            return await _context.Questions
                .Include(q => q.AskedBy)
                .Where(q => q.IsFeatured && q.IsAnswered && (q.Audience == audience || q.Audience == Audience.Both))
                .OrderByDescending(q => q.AnsweredDate)
                .ToListAsync();
        }

        public async Task<Question> GetQuestionByIdAsync(int id)
        {
            return await _context.Questions
                .Include(q => q.AskedBy) // Include the user who asked the question
                .FirstOrDefaultAsync(q => q.Id == id);
        }
        public async Task UpdateQuestionAsync(Question question)
        {
            _context.Questions.Update(question);
            await _context.SaveChangesAsync();
        }
        public async Task DeleteQuestionAsync(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
                await _context.SaveChangesAsync();
            }
        }
    }
}