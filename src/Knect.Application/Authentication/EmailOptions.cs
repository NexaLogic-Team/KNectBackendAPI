namespace Knect.Application.Authentication;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "LogOnly";
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string From { get; set; } = "noreply@example.com";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
