using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FUMiniHotel.Areas.Identity.Data;
using FUMiniHotel.Models;
using FUMiniHotel.Repositories.IRepositories;
using System.Threading.Tasks;

namespace FUMiniHotel.Controllers
{
    public class QnAController : Controller
    {
        private readonly IQuestionRepository _questionRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public QnAController(IQuestionRepository questionRepository, UserManager<ApplicationUser> userManager)
        {
            _questionRepository = questionRepository;
            _userManager = userManager;
        }

        // GET: QnA/ContactUs
        public async Task<IActionResult> ContactUs()
        {
            var answeredQuestions = await _questionRepository.GetAllQuestionsAsync();
            answeredQuestions = answeredQuestions.Where(q => q.IsAnswered).ToList();
            return View(answeredQuestions);
        }

        // POST: QnA/ContactUs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ContactUs(string questionText)
        {
            if (string.IsNullOrWhiteSpace(questionText))
            {
                ModelState.AddModelError("questionText", "Question text cannot be empty.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            var question = new Question
            {
                QuestionText = questionText,
                AskedById = user?.Id,
                AskedDate = DateTime.UtcNow,
                IsAnswered = false,
                IsFeatured = false,
                Audience = Audience.Both
            };

            await _questionRepository.AddQuestionAsync(question);
            return RedirectToAction(nameof(ContactUs));
        }

        // GET: QnA (Public Q&A page)
        public async Task<IActionResult> Index()
        {
            var guestQuestions = await _questionRepository.GetFeaturedQuestionsForAudienceAsync(Audience.Guest);
            var hotelOwnerQuestions = await _questionRepository.GetFeaturedQuestionsForAudienceAsync(Audience.HotelOwner);
            var model = new QnAViewModel
            {
                GuestQuestions = guestQuestions,
                HotelOwnerQuestions = hotelOwnerQuestions
            };
            return View(model);
        }

        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Manage()
        {
            var questions = await _questionRepository.GetAllQuestionsAsync();
            return View(questions);
        }

        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Answer(int id)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }
            return View(question);
        }

        // POST: QnA/Answer/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Answer(int id, string answerText, bool isFeatured, Audience audience)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(answerText))
            {
                ModelState.AddModelError("", "Answer text cannot be empty.");
                return View(question);
            }

            var user = await _userManager.GetUserAsync(User);
            question.AnswerText = answerText;
            question.AnsweredById = user.Id;
            question.AnsweredDate = DateTime.UtcNow;
            question.IsAnswered = true;
            question.IsFeatured = isFeatured;
            question.Audience = audience;

            await _questionRepository.UpdateQuestionAsync(question);
            return RedirectToAction(nameof(Manage));
        }

        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> EditAnswer(int id)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            if (question == null || !question.IsAnswered)
            {
                return NotFound();
            }
            return View(question);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> EditAnswer(int id, string answerText, bool isFeatured, Audience audience)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            if (question == null || !question.IsAnswered)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(answerText))
            {
                ModelState.AddModelError("", "Answer text cannot be empty.");
                return View(question);
            }

            var user = await _userManager.GetUserAsync(User);
            question.AnswerText = answerText;
            question.AnsweredById = user.Id;
            question.AnsweredDate = DateTime.UtcNow;
            question.IsFeatured = isFeatured;
            question.Audience = audience;

            await _questionRepository.UpdateQuestionAsync(question);
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> ToggleFeatured(int id, Audience audience)
        {
            var question = await _questionRepository.GetQuestionByIdAsync(id);
            if (question == null)
            {
                return NotFound();
            }

            question.IsFeatured = !question.IsFeatured;
            question.Audience = audience;
            await _questionRepository.UpdateQuestionAsync(question);
            return RedirectToAction(nameof(Manage));
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _questionRepository.DeleteQuestionAsync(id);
            return RedirectToAction(nameof(Manage));
        }
    }
}