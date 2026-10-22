using System.Net;
using System.Net.Mail;
using Knect.Application.Authentication;
using Knect.Application.Authentication.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Knect.Infrastructure.Authentication;

public sealed class EmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> options, ILogger<EmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOtpAsync(string email, string purpose, string code, CancellationToken cancellationToken)
    {
        if (string.Equals(_options.Provider, "LogOnly", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("OTP request received for email {Email} with purpose {Purpose}. Code was generated and stored server-side.", email, purpose);
            return;
        }

        await SendEmailAsync(email, $"OTP for {purpose}", $"Your verification code is {code}.", cancellationToken);
    }

    public async Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email recipient is required.", nameof(email));
        }

        if (string.Equals(_options.Provider, "LogOnly", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Sending email to {Email} subject {Subject}.", email, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
        {
            throw new InvalidOperationException("SMTP email configuration is incomplete.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };
        message.To.Add(email);

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.Username, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        await client.SendMailAsync(message, cancellationToken);
        _logger.LogInformation("Email delivered to {Email} subject {Subject}.", email, subject);
    }
}
