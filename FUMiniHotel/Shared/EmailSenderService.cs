using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace FUMiniHotel.Services
{
    public class EmailSenderService : IEmailSender
    {
        private readonly ILogger<EmailSenderService> _logger;

        public EmailSenderService(ILogger<EmailSenderService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Log the email details (for testing purposes)
            _logger.LogInformation($"Sending email to {email} with subject '{subject}' and message: {htmlMessage}");
            return Task.CompletedTask;
        }
    }
}