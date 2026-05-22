using Microsoft.Extensions.Logging;
using RetroMask.Application.Abstractions;

namespace RetroMask.Infrastructure.Email;

public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL] To: {To} | Subject: {Subject}\n{Body}", to, subject, htmlBody);
        return Task.CompletedTask;
    }

    public Task SendTemplatedAsync(string to, string templateName, object model, CancellationToken ct = default)
    {
        _logger.LogInformation("[EMAIL TEMPLATE] To: {To} | Template: {Template} | Model: {Model}", to, templateName, model);
        return Task.CompletedTask;
    }
}
