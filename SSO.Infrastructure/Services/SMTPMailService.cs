using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SSO.Application.Configuaration;
using SSO.Application.Interfaces.Services;
using SSO.Application.Requests;

namespace SSO.Infrastructure.Services
{
    public class SMTPMailService : IMailService
    {
        private readonly MailConfiguration _config;
        private readonly ILogger<SMTPMailService> _logger;

        public SMTPMailService(IOptions<MailConfiguration> config, ILogger<SMTPMailService> logger)
        {
            _config = config.Value;
            _logger = logger;
        }

        public async Task SendAsync(MailRequest request)
        {
            try
            {
                MimeMessage email = new()
                {
                    Sender = new MailboxAddress(_config.DisplayName, request.From ?? _config.From),
                    Subject = request.Subject,
                    Body = new BodyBuilder
                    {
                        HtmlBody = request.Body
                    }.ToMessageBody()
                };
                email.From.Add(new MailboxAddress(_config.DisplayName, request.From ?? _config.From));
                email.To.AddRange(request.To.Select(x => MailboxAddress.Parse(x)).ToList());

                using SmtpClient smtp = new();
                // Use Auto to let MailKit decide the best security option based on the port
                await smtp.ConnectAsync(_config.Host, _config.Port, SecureSocketOptions.Auto);
                await smtp.AuthenticateAsync(_config.UserName, _config.Password);
                _ = await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }
    }
}
