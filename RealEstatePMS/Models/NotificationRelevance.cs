using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Models;

public static class NotificationRelevance
{
    /// <summary>Which notification types are meaningful to surface as a toggle for each role.</summary>
    private static readonly Dictionary<string, NotificationType[]> ByRole = new()
    {
        [Roles.Admin] = new[]
        {
            NotificationType.PendingPayment, NotificationType.UpcomingPayment, NotificationType.ExpiringContract,
            NotificationType.DelayedProject, NotificationType.NewSale, NotificationType.NewCustomer, NotificationType.PaymentReceived
        },
        [Roles.ProjectManager] = new[]
        {
            NotificationType.DelayedProject, NotificationType.PendingPayment, NotificationType.ExpiringContract, NotificationType.PaymentReceived
        },
        [Roles.SalesAgent] = new[]
        {
            NotificationType.NewSale, NotificationType.NewCustomer, NotificationType.ExpiringContract, NotificationType.PaymentReceived
        },
        [Roles.Accountant] = new[]
        {
            NotificationType.PendingPayment, NotificationType.UpcomingPayment, NotificationType.ExpiringContract, NotificationType.PaymentReceived
        },
        [Roles.Customer] = new[]
        {
            NotificationType.NewSale, // reused for booking confirmed/declined updates
            NotificationType.PaymentReceived
        },
    };

    private static readonly (NotificationType Type, string Label, string Description)[] Descriptions =
    {
        (NotificationType.PendingPayment, "Pending Payments", "A sale or rental has an outstanding balance."),
        (NotificationType.UpcomingPayment, "Upcoming Payments", "A payment is coming due soon."),
        (NotificationType.ExpiringContract, "Expiring Contracts", "A rental contract is expiring within 30 days."),
        (NotificationType.DelayedProject, "Delayed Projects", "A project has passed its expected end date."),
        (NotificationType.NewSale, "New Sales / Booking Updates", "A new sale is created, or one of your booking requests is confirmed/declined."),
        (NotificationType.NewCustomer, "New Customers", "A new customer registers in the system."),
        (NotificationType.PaymentReceived, "Payment Received", "A payment is recorded against a sale or rental."),
    };

    public static List<(NotificationType Type, string Label, string Description)> GetForRoles(IEnumerable<string> roles)
    {
        var types = new HashSet<NotificationType>();
        foreach (var role in roles)
        {
            if (ByRole.TryGetValue(role, out var roleTypes))
            {
                foreach (var t in roleTypes) types.Add(t);
            }
        }

        return Descriptions.Where(d => types.Contains(d.Type)).ToList();
    }
}
