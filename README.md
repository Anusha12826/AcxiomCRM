# ACXIOMCRM – Customer Relationship Management System

A production-style, role-governed Customer Relationship Management (CRM) web application built with **ASP.NET Core 8.0 MVC**, **Entity Framework Core**, **ASP.NET Core Identity**, **Razor Views**, **Bootstrap 5**, **Chart.js**, and **ASP.NET Core Web API / OpenAPI (Swagger)**.

---

## 1. Architectural Overview

AcxiomCRM is engineered following a clean layered architecture with separation of concerns:

```
Presentation Layer (Razor Views, ViewModels, Controllers, REST Web API, Swagger)
         ↓
Application Layer (CRM Business Services, DTOs, Mapping, Security Scoping)
         ↓
Domain / Business Layer (Entities, Validation Rules, Lifecycle Workflows)
         ↓
Data Access Layer (ApplicationDbContext, EF Core Dual-Provider, Migrations, Seed Initializer)
         ↓
Database (SQL Server / SQLite with Foreign Keys, Indexes & Cascade Restrictions)
```

Cross-cutting concerns (**Centralized Append-Only Auditing**, **Server-Side Validation**, **Role-Based Authorization**, and **Anti-Forgery Protection**) are enforced across all layers.

---

## 2. Technology Stack

- **Framework**: .NET 8.0 / C# 12
- **Presentation**: ASP.NET Core MVC, Razor View Engine, Bootstrap 5, Bootstrap Icons, Chart.js
- **Data Access**: Entity Framework Core 8.0 (Dual-Provider: SQLite for zero-config local runs, SQL Server for enterprise deployments)
- **Security & Authentication**: ASP.NET Core Identity (PBKDF2 password hashing, lockout policy, cookie-based session management)
- **REST APIs**: ASP.NET Core Web API Controllers with OpenAPI / Swagger UI
- **Auditing**: Centralized `AuditService` logging security events and CRM entity state mutations

---

## 3. Seeded Accounts & Role Permissions Matrix

The database is automatically seeded on startup with the following test credentials:

| Role | Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin@acxiomcrm.com` | `Admin@12345` | Full system administration, user & role management, immutable audit log viewer, all CRM records, reports, and system configuration. |
| **Manager** | `manager@acxiomcrm.com` | `Manager@12345` | Team pipeline oversight, all CRM records, performance & user activity reports; strictly no security administration. |
| **SalesExecutive** | `sales@acxiomcrm.com` | `Sales@12345` | Scoped to assigned customers, leads, opportunities, follow-ups, and activities; permitted sales dashboard & scoped reports. |

---

## 4. Database Entities & Core Schema

1. **User (`ApplicationUser`)**: Extends `IdentityUser` with `FullName`, `CreatedDate`, `IsActive`.
2. **Role (`ApplicationRole`)**: Extends `IdentityRole` with `Description`.
3. **Customer**: `CustomerId`, `CustomerCode` (Auto `CUST-xxxx`), `CustomerName`, `Email`, `Phone`, `CompanyName`, `Address`, `City`, `State`, `Status`, `CreatedDate`, `CreatedBy`, `AssignedTo`.
4. **Lead**: `LeadId`, `LeadCode` (Auto `LEAD-xxxx`), `LeadName`, `Email`, `Phone`, `CompanyName`, `Source`, `Status` (`New`, `Contacted`, `Qualified`, `Unqualified`, `Converted`, `Lost`), `Priority`, `ExpectedValue`, `AssignedTo`, `ConvertedCustomerId`, `ConvertedOpportunityId`.
5. **Opportunity**: `OpportunityId`, `OpportunityName`, `CustomerId`, `LeadId`, `Amount`, `Stage` (`Qualification`, `Proposal`, `Negotiation`, `Won`, `Lost`), `Probability` (0–100%), `ExpectedCloseDate`, `Status` (`Open`, `Won`, `Lost`), `CreatedDate`, `AssignedTo`.
6. **FollowUp**: `FollowUpId`, `CustomerId`, `LeadId`, `FollowUpDate`, `FollowUpType` (`Call`, `Meeting`, `Email`, `Task`), `Remarks`, `Status` (`Planned`, `Completed`, `Missed`, `Cancelled`), `AssignedTo`.
7. **Activity**: `ActivityId`, `ActivityType` (`Call`, `Meeting`, `Email`, `Task`), `Subject`, `Description`, `ActivityDate`, `CustomerId`, `LeadId`, `AssignedTo`, `Status` (`Pending`, `Completed`, `Cancelled`).
8. **AuditLog**: `AuditLogId`, `UserId`, `Action`, `EntityName`, `RecordId`, `OldValue`, `NewValue`, `CreatedDate`, `IpAddress`, `Result`, `Details`.

---

## 5. Core CRM Workflows & Business Rules

### Lead Qualification & Conversion Workflow
1. Lead is captured (`Status: New` $\rightarrow$ `Contacted` $\rightarrow$ `Qualified`).
2. Conversion trigger creates an active `Customer` record (auto-generating `CustomerCode`).
3. Optionally instantiates a qualified `Opportunity` with deal valuation and close date.
4. Updates Lead status to `Converted`, links `ConvertedCustomerId` & `ConvertedOpportunityId`.
5. Logs immutable audit entries (`LeadConverted`, `CustomerCreated`, `OpportunityCreated`).

### Opportunity Pipeline & Forecast Calculations
- **Active Deal Validation**: `Amount > 0`, `Probability` between 0% and 100%, `ExpectedCloseDate` cannot be in the past.
- **Weighted Pipeline Formula**:
  $$\text{Weighted Pipeline} = \text{Amount} \times \frac{\text{Probability}}{100}$$
- **Stage Transitions**: Transitioning to `Won` automatically locks probability to 100% and status to `Won`; `Lost` locks probability to 0% and status to `Lost`.

### Follow-Up Compliance Workflow
- Follow-up date for new or planned reminders cannot be in the past ($$\text{FollowUpDate} \ge \text{Today}$$).
- Quick action endpoints support immediate completion and rescheduling with mandatory audit log records.

---

## 6. Business Reports & Analytics Module

Accessible via `/Reports` with role-based scoping:

1. **Customer Report** (`/Reports/Customers`): Complete account directory, geographic analysis, assigned owners, CSV export.
2. **Lead Report** (`/Reports/Leads`): Channel acquisition effectiveness, qualification stages, expected deal valuations, CSV export.
3. **Follow-Up Compliance Report** (`/Reports/FollowUps`): Planned, overdue, and completed client interactions, CSV export.
4. **Opportunity Forecast Report** (`/Reports/Opportunities`): Deal milestones, probability, weighted values, CSV export.
5. **Sales Pipeline Matrix** (`/Reports/Pipeline`): Stage-wise and representative-wise distributions, weighted totals, CSV export.
6. **Conversion Report** (`/Reports/Conversion`): Lead conversion percentages, win/loss deal counts, total realized won revenue.
7. **User Activity Report** (`/Reports/UserActivity`): Representative interaction breakdown (Calls, Meetings, Emails, Tasks) *(Manager/Admin only)*.
8. **Audit Compliance Report** (`/Reports/Audit`): Security events, authentication logs, and data mutation chronologies *(Admin only)*.

---

## 7. REST APIs & OpenAPI / Swagger

Interactive Swagger UI documentation is available at:
```
http://localhost:5175/swagger
```

All API endpoints return standard enveloped responses (`ApiResponse<T>`):

- `GET /api/customers` – List customers (supports search, filter, pagination)
- `GET /api/customers/{id}` – Get customer by ID
- `POST /api/customers` – Create customer with duplicate email/phone validation
- `PUT /api/customers/{id}` – Update customer
- `DELETE /api/customers/{id}` – Deactivate customer (Manager/Admin)
- `GET /api/leads` – List leads
- `POST /api/leads` – Create lead
- `PUT /api/leads/{id}` – Update lead
- `POST /api/leads/{id}/convert` – Convert lead to Customer and Opportunity
- `GET /api/opportunities` – List opportunities
- `POST /api/opportunities` – Create opportunity with business rules
- `PATCH /api/opportunities/{id}/stage` – Update stage with probability auto-sync
- `GET /api/reports/pipeline` – Real-time pipeline stage & owner matrix data

---

## 8. Setup & Execution Guide

### Prerequisites
- .NET 8.0 SDK installed
- Windows PowerShell or terminal

### Configuration
In `appsettings.json`, select your preferred database provider:
- **SQLite (Default for quick local start)**:
  ```json
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=AcxiomCRM.db"
  }
  ```
- **SQL Server**:
  ```json
  "DatabaseProvider": "SqlServer",
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AcxiomCRM;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
  ```

### Running the Application
```powershell
# Restore dependencies and build
dotnet build

# Launch application
dotnet run --no-build
```

Access the application in your browser:
```
http://localhost:5175
```

---

## 9. Automated Test Verification Suite

The repository includes end-to-end integration test scripts validating every layer and business rule against the live database:

| Script | Phase / Capability | Status |
| :--- | :--- | :--- |
| `test_phase2.ps1` | Authentication, Role Access, Password Lockout, 403 Access Denied | **14 / 14 PASS** |
| `test_phase4.ps1` | Customer Management, Auto Code, Unique Email/Phone, Audit Logging | **10 / 10 PASS** |
| `test_phase5.ps1` | Lead Management, Status Transitions, Lead Conversion Workflow | **10 / 10 PASS** |
| `test_phase6.ps1` | Follow-Up Scheduling, Overdue Detection, Complete & Reschedule | **10 / 10 PASS** |
| `test_phase7.ps1` | Opportunity Pipeline, Weighted Value, Business Validation, Stage Sync | **11 / 11 PASS** |
| `test_phase8.ps1` | Activity Management (Calls, Meetings, Emails, Tasks) | **9 / 9 PASS** |
| `test_phase10.ps1` | Centralized Append-Only Audit Viewer & Security Enforcements | **8 / 8 PASS** |
| `test_phase11.ps1` | REST APIs & OpenAPI / Swagger Endpoints | **15 / 15 PASS** |
| `test_phase12.ps1` | Reports Module (8 Reports, Role Scoping, CSV Exports) | **17 / 17 PASS** |
| `test_acceptance_scenarios.ps1` | Section 17.19 Comprehensive Final Acceptance Verification | **14 / 14 PASS** |

To execute the final acceptance suite at any time:
```powershell
powershell -ExecutionPolicy Bypass -File .\test_acceptance_scenarios.ps1
```
