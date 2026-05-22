using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RetroMask.Application.Abstractions;
using System.Net;
using System.Net.Mail;

namespace RetroMask.Infrastructure.Email;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_configuration["Smtp:From"] ?? throw new InvalidOperationException("SMTP From not configured.")),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        using var client = new SmtpClient(
            _configuration["Smtp:Host"] ?? throw new InvalidOperationException("SMTP Host not configured."),
            int.Parse(_configuration["Smtp:Port"] ?? "587"))
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(
                _configuration["Smtp:Username"],
                _configuration["Smtp:Password"])
        };

        await client.SendMailAsync(message, ct);
        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }

    public async Task SendTemplatedAsync(string to, string templateName, object model, CancellationToken ct = default)
    {
        // TODO: Implement template rendering (e.g., Razor or Scriban)
        await SendAsync(to, templateName, model.ToString() ?? string.Empty, ct);
    }
}
