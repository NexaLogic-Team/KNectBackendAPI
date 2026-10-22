namespace Knect.Application.Authentication.Interfaces;

public interface IEmailSender
{
    Task SendOtpAsync(string email, string purpose, string code, CancellationToken cancellationToken);
    Task SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken);
}
