using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using TicketNepal.Identity.Application.Abstractions;

namespace TicketNepal.Api.Tests;

public sealed partial class FakeEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _sent = new();

    public IReadOnlyCollection<EmailMessage> Sent => _sent;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _sent.Enqueue(message);
        return Task.CompletedTask;
    }

    public (Guid UserId, string Token) GetConfirmation(string to)
    {
        var body = _sent.Last(m => m.To == to).HtmlBody;
        var match = ConfirmationLink().Match(body);
        return (Guid.Parse(match.Groups["id"].Value), match.Groups["token"].Value);
    }

    [GeneratedRegex(@"userId=(?<id>[0-9a-fA-F-]{36})&token=(?<token>[A-Za-z0-9_-]+)")]
    private static partial Regex ConfirmationLink();
}