namespace RealEstatePMS.Models;

public static class Modules
{
    public const string Projects = "Projects";
    public const string Properties = "Properties";
    public const string Customers = "Customers";
    public const string Sales = "Sales";
    public const string Rentals = "Rentals";
    public const string Payments = "Payments";
    public const string Tasks = "Tasks";
    public const string Documents = "Documents";
    public const string Reports = "Reports";
    public const string BookingRequests = "BookingRequests";

    public static readonly (string Key, string DisplayName, string Icon)[] All =
    {
        (Projects, "Projects", "bi-kanban"),
        (Properties, "Properties", "bi-house-door"),
        (Customers, "Customers", "bi-people"),
        (Sales, "Sales", "bi-cash-coin"),
        (Rentals, "Rentals", "bi-key"),
        (Payments, "Payments", "bi-credit-card"),
        (Tasks, "Tasks", "bi-list-check"),
        (Documents, "Documents", "bi-file-earmark-text"),
        (Reports, "Reports", "bi-graph-up"),
        (BookingRequests, "Booking Requests", "bi-calendar-check"),
    };

    // Roles whose access can be configured from the Settings page.
    // Admin always has full access and is not configurable, for safety.
    public static readonly string[] ConfigurableRoles =
    {
        RealEstatePMS.Models.Roles.ProjectManager,
        RealEstatePMS.Models.Roles.SalesAgent,
        RealEstatePMS.Models.Roles.Accountant,
    };
}
