using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace ManokshaApi.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpHost = _config["Smtp:Host"];
            var smtpPort = int.Parse(_config["Smtp:Port"] ?? "587");
            var smtpUser = _config["Smtp:User"];
            var smtpPass = _config["Smtp:Pass"];
            var from = _config["Smtp:From"] ?? smtpUser;

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("Manoksha Collections", from));
            msg.To.Add(MailboxAddress.Parse(toEmail));
            msg.Subject = subject;
            msg.Body = new TextPart("html") { Text = htmlBody };

            int maxRetries = 3;
            int delayMs = 2000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var client = new SmtpClient();
                    await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);

                    if (!string.IsNullOrEmpty(smtpUser))
                        await client.AuthenticateAsync(smtpUser, smtpPass);

                    await client.SendAsync(msg);
                    await client.DisconnectAsync(true);

                    _logger.LogInformation($"✅ Email sent successfully to {toEmail}");
                    return;
                }
                catch (AuthenticationException ex)
                {
                    _logger.LogError($"❌ Authentication failed: {ex.Message}. Check App Password or 2FA settings.");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"❌ Error sending email (Attempt {attempt}/{maxRetries}): {ex.Message}");
                    if (attempt < maxRetries)
                        await Task.Delay(delayMs);
                    else
                        throw;
                }
            }
        }
    }
}
