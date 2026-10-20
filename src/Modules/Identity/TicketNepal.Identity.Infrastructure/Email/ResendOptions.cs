namespace TicketNepal.Identity.Infrastructure.Email;

public sealed class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string From { get; set; } = "TicketNepal <onboarding@resend.dev>";
}