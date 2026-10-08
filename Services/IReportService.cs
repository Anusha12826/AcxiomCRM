using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services
{
    public interface IReportService
    {
        Task<GenericReportViewModel<CustomerReportItem>> GetCustomerReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<GenericReportViewModel<LeadReportItem>> GetLeadReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<GenericReportViewModel<FollowUpReportItem>> GetFollowUpReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<GenericReportViewModel<OpportunityReportItem>> GetOpportunityReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<PipelineReportViewModel> GetPipelineReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<ConversionReportViewModel> GetConversionReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<UserActivityReportViewModel> GetUserActivityReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<AuditReportViewModel> GetAuditReportAsync(
            string userId, string role, ReportFilterViewModel filter);

        Task<Dictionary<string, string>> GetSalesUsersAsync();
    }
}
