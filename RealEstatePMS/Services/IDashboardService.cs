using RealEstatePMS.ViewModels;

namespace RealEstatePMS.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> BuildAsync();
    Task<ProjectManagerDashboardViewModel> BuildForProjectManagerAsync(string userId);
    Task<SalesAgentDashboardViewModel> BuildForSalesAgentAsync(string userId);
    Task<AccountantDashboardViewModel> BuildForAccountantAsync();
    Task<CustomerDashboardViewModel> BuildForCustomerAsync(string userId);
}
