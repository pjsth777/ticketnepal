namespace TicketNepal.SharedKernel.Security;

public static class Role
{
    public const string Attendee = "Attendee";
    public const string Organizer = "Organizer";
    public const string Admin = "Admin";

    public static readonly string[] All = [Attendee, Organizer, Admin];
}