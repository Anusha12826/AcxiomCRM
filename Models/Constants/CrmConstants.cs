namespace AcxiomCRM.Models.Constants
{
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string SalesExecutive = "SalesExecutive";

        public static readonly string[] AllRoles = [Admin, Manager, SalesExecutive];
    }

    public static class LeadStatuses
    {
        public const string New = "New";
        public const string Contacted = "Contacted";
        public const string Qualified = "Qualified";
        public const string Unqualified = "Unqualified";
        public const string Converted = "Converted";
        public const string Lost = "Lost";

        public static readonly string[] AllStatuses = [New, Contacted, Qualified, Unqualified, Converted, Lost];
    }

    public static class OpportunityStages
    {
        public const string Qualification = "Qualification";
        public const string Proposal = "Proposal";
        public const string Negotiation = "Negotiation";
        public const string Won = "Won";
        public const string Lost = "Lost";

        public static readonly string[] AllStages = [Qualification, Proposal, Negotiation, Won, Lost];
    }

    public static class OpportunityStatuses
    {
        public const string Open = "Open";
        public const string Won = "Won";
        public const string Lost = "Lost";

        public static readonly string[] AllStatuses = [Open, Won, Lost];
    }

    public static class FollowUpStatuses
    {
        public const string Planned = "Planned";
        public const string Completed = "Completed";
        public const string Missed = "Missed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] AllStatuses = [Planned, Completed, Missed, Cancelled];
    }

    public static class FollowUpTypes
    {
        public const string Call = "Call";
        public const string Meeting = "Meeting";
        public const string Email = "Email";

        public static readonly string[] AllTypes = [Call, Meeting, Email];
    }

    public static class ActivityTypes
    {
        public const string Call = "Call";
        public const string Meeting = "Meeting";
        public const string Email = "Email";
        public const string Task = "Task";

        public static readonly string[] AllTypes = [Call, Meeting, Email, Task];
    }

    public static class ActivityStatuses
    {
        public const string Completed = "Completed";
        public const string Pending = "Pending";
        public const string Cancelled = "Cancelled";

        public static readonly string[] AllStatuses = [Completed, Pending, Cancelled];
    }

    public static class CustomerStatuses
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";

        public static readonly string[] AllStatuses = [Active, Inactive];
    }
}
