using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TicketNepal.Identity.Application.Abstractions;

namespace TicketNepal.Identity.Infrastructure.Email;

internal sealed class ResendEmailSender(
    HttpClient http, IOptions<ResendOptions> options, ILogger<ResendEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var payload = new
        {
            from = options.Value.From,
            to = new[] { message.To },
            subject = message.Subject,
            html = message.HtmlBody
        };

        using var response = await http.PostAsJsonAsync("emails", payload, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError("Resend rejected the email: {Status} {Body}", (int)response.StatusCode, body);
        throw new InvalidOperationException($"Resend returned {(int)response.StatusCode}.");
    }
}