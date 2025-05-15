using FUMiniHotel.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FUMiniHotel.Repositories.IRepositories
{
    public interface IQuestionRepository
    {
        Task AddQuestionAsync(Question question);
        Task<List<Question>> GetAllQuestionsAsync();
        Task<List<Question>> GetFeaturedQuestionsForAudienceAsync(Audience audience);
        Task<Question> GetQuestionByIdAsync(int id);
        Task UpdateQuestionAsync(Question question);
        Task DeleteQuestionAsync(int id);
    }
}