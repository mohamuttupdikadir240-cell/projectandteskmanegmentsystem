namespace RealEstatePMS.Models;

public static class Roles
{
    public const string Admin = "Admin";
    public const string ProjectManager = "ProjectManager";
    public const string SalesAgent = "SalesAgent";
    public const string Accountant = "Accountant";
    public const string Customer = "Customer";

    public static readonly string[] All = { Admin, ProjectManager, SalesAgent, Accountant, Customer };

    public const string ManagementRoles = Admin + "," + ProjectManager;
    public const string SalesRoles = Admin + "," + ProjectManager + "," + SalesAgent;
    public const string FinanceRoles = Admin + "," + Accountant;
    public const string StaffRoles = Admin + "," + ProjectManager + "," + SalesAgent + "," + Accountant;
}
