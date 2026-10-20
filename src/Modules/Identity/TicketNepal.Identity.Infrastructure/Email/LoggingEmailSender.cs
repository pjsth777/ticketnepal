using Microsoft.Extensions.Logging;
using TicketNepal.Identity.Application.Abstractions;

namespace TicketNepal.Identity.Infrastructure.Email;

internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        logger.LogInformation("EMAIL (not sent) To={To} Subject={Subject}\n{Body}",
            message.To, message.Subject, message.HtmlBody);
        return Task.CompletedTask;
    }
}