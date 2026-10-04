using RealEstatePMS.Models.Enums;

namespace RealEstatePMS.Helpers;

public static class StatusBadge
{
    public static string Project(ProjectStatus status) => status switch
    {
        ProjectStatus.Planning => "badge-soft-info",
        ProjectStatus.InProgress => "badge-soft-warning",
        ProjectStatus.Completed => "badge-soft-success",
        ProjectStatus.OnHold => "badge-soft-secondary",
        ProjectStatus.Cancelled => "badge-soft-danger",
        _ => "badge-soft-secondary"
    };

    public static string Property(PropertyStatus status) => status switch
    {
        PropertyStatus.Available => "badge-soft-success",
        PropertyStatus.Reserved => "badge-soft-warning",
        PropertyStatus.Sold => "badge-soft-danger",
        PropertyStatus.Rented => "badge-soft-info",
        PropertyStatus.UnderConstruction => "badge-soft-secondary",
        _ => "badge-soft-secondary"
    };

    public static string Payment(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "badge-soft-secondary",
        PaymentStatus.Partial => "badge-soft-warning",
        PaymentStatus.Paid => "badge-soft-success",
        PaymentStatus.Overdue => "badge-soft-danger",
        _ => "badge-soft-secondary"
    };

    public static string Contract(ContractStatus status) => status switch
    {
        ContractStatus.Active => "badge-soft-success",
        ContractStatus.Expired => "badge-soft-danger",
        ContractStatus.Terminated => "badge-soft-secondary",
        ContractStatus.Renewed => "badge-soft-info",
        _ => "badge-soft-secondary"
    };

    public static string TaskStatus(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Pending => "badge-soft-secondary",
        ProjectTaskStatus.InProgress => "badge-soft-warning",
        ProjectTaskStatus.Completed => "badge-soft-success",
        ProjectTaskStatus.Delayed => "badge-soft-danger",
        _ => "badge-soft-secondary"
    };

    public static string Priority(TaskPriority priority) => priority switch
    {
        TaskPriority.Low => "badge-soft-secondary",
        TaskPriority.Medium => "badge-soft-info",
        TaskPriority.High => "badge-soft-warning",
        TaskPriority.Urgent => "badge-soft-danger",
        _ => "badge-soft-secondary"
    };

    public static string Document(DocumentType type) => "badge-soft-info";

    public static string Booking(BookingStatus status) => status switch
    {
        BookingStatus.Pending => "badge-soft-warning",
        BookingStatus.Confirmed => "badge-soft-success",
        BookingStatus.Rejected => "badge-soft-danger",
        _ => "badge-soft-secondary"
    };
}
