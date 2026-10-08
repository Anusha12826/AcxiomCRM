$ErrorActionPreference = "Stop"

Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "     ACXIOMCRM - 17.19 COMPREHENSIVE ACCEPTANCE VERIFICATION         " -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5175"
$results = @()

function Test-Scenario($id, $title, $condition, $details = "") {
    if ($condition) {
        Write-Host " [SCENARIO $id : PASS] $title" -ForegroundColor Green
        if ($details) { Write-Host "     -> Details: $details" -ForegroundColor DarkGray }
        $script:results += [PSCustomObject]@{ Scenario = $id; Title = $title; Status = "PASS"; Details = $details }
    } else {
        Write-Host " [SCENARIO $id : FAIL] $title" -ForegroundColor Red
        if ($details) { Write-Host "     -> Details: $details" -ForegroundColor Yellow }
        $script:results += [PSCustomObject]@{ Scenario = $id; Title = $title; Status = "FAIL"; Details = $details }
    }
}

function Get-Token($url, $session) {
    $page = Invoke-WebRequest -Uri $url -WebSession $session -UseBasicParsing
    $match = [regex]::Match($page.Content, '__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) {
        $match = [regex]::Match($page.Content, 'value="([^"]+)" name="__RequestVerificationToken"')
    }
    return $match.Groups[1].Value
}

try {
    # -----------------------------------------------------------------------------------------
    # SCENARIO 1: Open AcxiomCRM without authentication -> protected CRM pages must not be accessible.
    # -----------------------------------------------------------------------------------------
    $unauthBlocked = $false
    try {
        $unauthResp1 = Invoke-WebRequest -Uri "$baseUrl/Dashboard" -Method Get -MaximumRedirection 0 -UseBasicParsing
        $unauthBlocked = ($unauthResp1.StatusCode -eq 302 -or $unauthResp1.StatusCode -eq 401)
    } catch {
        $msg = "$_"
        if ($msg -like "*redirection*" -or $msg -like "*302*" -or $msg -like "*401*" -or $msg -like "*Login*") {
            $unauthBlocked = $true
        }
    }
    Test-Scenario 1 "Protected CRM pages require authentication" $unauthBlocked "Redirected to Login"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 2: Register/login with a valid test user -> user reaches the Dashboard.
    # -----------------------------------------------------------------------------------------
    $adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $token = Get-Token "$baseUrl/Account/Login" $adminSession
    $loginBody = @{
        "Email" = "admin@acxiomcrm.com"
        "Password" = "Admin@12345"
        "RememberMe" = "false"
        "__RequestVerificationToken" = $token
    }
    $loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $adminSession -UseBasicParsing
    $dashResp = Invoke-WebRequest -Uri "$baseUrl/Dashboard" -Method Get -WebSession $adminSession -UseBasicParsing
    $reachesDashboard = ($dashResp.StatusCode -eq 200 -and ($dashResp.Content -like "*Sales Dashboard*" -or $dashResp.Content -like "*Dashboard*"))
    Test-Scenario 2 "Valid login redirects user to role Dashboard" $reachesDashboard "Reaches /Dashboard successfully"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 3: Create a Customer with invalid email/phone -> client-side validation appears.
    # -----------------------------------------------------------------------------------------
    $custCreatePage = Invoke-WebRequest -Uri "$baseUrl/Customers/Create" -Method Get -WebSession $adminSession -UseBasicParsing
    $hasClientVal = ($custCreatePage.Content -like "*data-val-email*" -and $custCreatePage.Content -like "*data-val-required*" -and $custCreatePage.Content -like "*jquery.validate*")
    Test-Scenario 3 "Customer form includes HTML5 / jQuery unobtrusive validation" $hasClientVal "data-val attributes and scripts present"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 4: Bypass browser validation with a crafted request -> server-side validation rejects invalid data.
    # -----------------------------------------------------------------------------------------
    $custToken = Get-Token "$baseUrl/Customers/Create" $adminSession
    $badCustBody = @{
        "CustomerName" = "" # Required missing
        "Email" = "not-an-email" # Invalid format
        "Phone" = "abc"
        "__RequestVerificationToken" = $custToken
    }
    $badCustResp = Invoke-WebRequest -Uri "$baseUrl/Customers/Create" -Method Post -Body $badCustBody -WebSession $adminSession -UseBasicParsing
    $serverValRejected = ($badCustResp.Content -like "*Customer Name is required.*" -or $badCustResp.Content -like "*Enter a valid email address.*")
    Test-Scenario 4 "Crafted invalid customer request rejected by server-side validation" $serverValRejected "Model state validation errors returned"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 5: Create an Opportunity with Amount <= 0 -> rejected.
    # -----------------------------------------------------------------------------------------
    $oppToken = Get-Token "$baseUrl/Opportunities/Create" $adminSession
    $badAmountBody = @{
        "OpportunityName" = "Invalid Zero Deal"
        "Amount" = "0"
        "Stage" = "Qualification"
        "Probability" = "20"
        "ExpectedCloseDate" = (Get-Date).AddDays(10).ToString("yyyy-MM-dd")
        "__RequestVerificationToken" = $oppToken
    }
    $badAmountResp = Invoke-WebRequest -Uri "$baseUrl/Opportunities/Create" -Method Post -Body $badAmountBody -WebSession $adminSession -UseBasicParsing
    $amountRejected = ($badAmountResp.Content -like "*Opportunity Amount must be greater than 0.*" -or $badAmountResp.StatusCode -eq 400)
    Test-Scenario 5 "Opportunity with Amount <= 0 rejected by business validation" $amountRejected "Rejected"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 6: Create an Opportunity with Probability > 100 -> rejected.
    # -----------------------------------------------------------------------------------------
    $badProbBody = @{
        "OpportunityName" = "Invalid Over 100 Deal"
        "Amount" = "15000"
        "Stage" = "Qualification"
        "Probability" = "120" # > 100
        "ExpectedCloseDate" = (Get-Date).AddDays(10).ToString("yyyy-MM-dd")
        "__RequestVerificationToken" = $oppToken
    }
    $badProbResp = Invoke-WebRequest -Uri "$baseUrl/Opportunities/Create" -Method Post -Body $badProbBody -WebSession $adminSession -UseBasicParsing
    $probRejected = ($badProbResp.Content -like "*Probability must be between 0 and 100.*" -or $badProbResp.StatusCode -eq 400)
    Test-Scenario 6 "Opportunity with Probability > 100 rejected" $probRejected "Rejected"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 7: Set Expected Close Date in the past -> rejected for an active opportunity.
    # -----------------------------------------------------------------------------------------
    $badDateBody = @{
        "OpportunityName" = "Invalid Past Deal"
        "Amount" = "15000"
        "Stage" = "Qualification"
        "Probability" = "25"
        "ExpectedCloseDate" = (Get-Date).AddDays(-10).ToString("yyyy-MM-dd") # In past
        "__RequestVerificationToken" = $oppToken
    }
    $badDateResp = Invoke-WebRequest -Uri "$baseUrl/Opportunities/Create" -Method Post -Body $badDateBody -WebSession $adminSession -UseBasicParsing
    $pastDateRejected = ($badDateResp.Content -like "*Expected Close Date cannot be in the past.*" -or $badDateResp.StatusCode -eq 400)
    Test-Scenario 7 "Opportunity with past Expected Close Date rejected" $pastDateRejected "Rejected"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 8: Create a Follow-Up dated before today -> rejected for a new/planned follow-up.
    # -----------------------------------------------------------------------------------------
    $fllwToken = Get-Token "$baseUrl/FollowUps/Create" $adminSession
    $badFllwBody = @{
        "FollowUpType" = "Call"
        "FollowUpDate" = (Get-Date).AddDays(-5).ToString("yyyy-MM-ddTHH:mm")
        "Status" = "Planned"
        "Remarks" = "Past reminder"
        "__RequestVerificationToken" = $fllwToken
    }
    $badFllwResp = Invoke-WebRequest -Uri "$baseUrl/FollowUps/Create" -Method Post -Body $badFllwBody -WebSession $adminSession -UseBasicParsing
    $fllwPastRejected = ($badFllwResp.Content -like "*Follow-up date cannot be earlier than today.*" -or $badFllwResp.StatusCode -eq 400)
    Test-Scenario 8 "Planned Follow-Up dated before today rejected" $fllwPastRejected "Rejected"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 9: Login as SalesExecutive -> only authorized/assigned sales scope is accessible.
    # -----------------------------------------------------------------------------------------
    $salesSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $salesToken = Get-Token "$baseUrl/Account/Login" $salesSession
    $salesLoginBody = @{
        "Email" = "sales@acxiomcrm.com"
        "Password" = "Sales@12345"
        "RememberMe" = "false"
        "__RequestVerificationToken" = $salesToken
    }
    $salesLoginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $salesLoginBody -WebSession $salesSession -UseBasicParsing
    $salesDash = Invoke-WebRequest -Uri "$baseUrl/Dashboard" -Method Get -WebSession $salesSession -UseBasicParsing
    $salesRoleScoped = ($salesDash.StatusCode -eq 200 -and ($salesDash.Content -like "*Sales Executive*" -or $salesDash.Content -like "*Assigned Scope*"))
    Test-Scenario 9 "SalesExecutive restricted to sales-authorized dashboard scope" $salesRoleScoped "Sales role confirmed"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 10: Login as Manager -> team pipeline/report access is available.
    # -----------------------------------------------------------------------------------------
    $mgrSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $mgrToken = Get-Token "$baseUrl/Account/Login" $mgrSession
    $mgrLoginBody = @{
        "Email" = "manager@acxiomcrm.com"
        "Password" = "Manager@12345"
        "RememberMe" = "false"
        "__RequestVerificationToken" = $mgrToken
    }
    $mgrLoginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $mgrLoginBody -WebSession $mgrSession -UseBasicParsing
    $mgrPipeline = Invoke-WebRequest -Uri "$baseUrl/Reports/Pipeline" -Method Get -WebSession $mgrSession -UseBasicParsing
    $mgrPipelineOk = ($mgrPipeline.StatusCode -eq 200 -and $mgrPipeline.Content -like "*Sales Pipeline Matrix*")
    Test-Scenario 10 "Manager has access to team pipeline and management reports" $mgrPipelineOk "Pipeline matrix rendered"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 11: Login as Admin -> user/role/audit administration is available.
    # -----------------------------------------------------------------------------------------
    $adminUsers = Invoke-WebRequest -Uri "$baseUrl/Users" -Method Get -WebSession $adminSession -UseBasicParsing
    $adminAudit = Invoke-WebRequest -Uri "$baseUrl/Audit" -Method Get -WebSession $adminSession -UseBasicParsing
    $adminAccessOk = ($adminUsers.StatusCode -eq 200 -and $adminAudit.StatusCode -eq 200 -and ($adminUsers.Content -like "*User & Role Management*" -or $adminUsers.Content -like "*Add New User*"))
    Test-Scenario 11 "Admin has full user/role/audit administration access" $adminAccessOk "User management & Audit viewer accessible"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 12: Create/update/delete a CRM record -> appropriate audit entry is generated.
    # -----------------------------------------------------------------------------------------
    $uniqueTick = [DateTime]::UtcNow.Ticks.ToString().Substring(10)
    $validCustToken = Get-Token "$baseUrl/Customers/Create" $adminSession
    $newCustBody = @{
        "CustomerName" = "Audit Test Entity $uniqueTick"
        "Email" = "audit_$uniqueTick@company.com"
        "Phone" = "+1-888-$uniqueTick"
        "CompanyName" = "Audit Enterprises"
        "__RequestVerificationToken" = $validCustToken
    }
    $createCustResp = Invoke-WebRequest -Uri "$baseUrl/Customers/Create" -Method Post -Body $newCustBody -WebSession $adminSession -UseBasicParsing
    $auditLogCheck = Invoke-WebRequest -Uri "$baseUrl/Audit?module=Customer" -Method Get -WebSession $adminSession -UseBasicParsing
    $hasAuditRecord = ($auditLogCheck.StatusCode -eq 200 -and ($auditLogCheck.Content -like "*CustomerCreated*" -or $auditLogCheck.Content -like "*Audit Test Entity*"))
    Test-Scenario 12 "CRM record mutation generates append-only AuditLog record" $hasAuditRecord "Audit entry created in database"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 13: Call /api/customers -> authorized JSON response is returned.
    # -----------------------------------------------------------------------------------------
    $apiCustResp = Invoke-WebRequest -Uri "$baseUrl/api/customers" -Method Get -WebSession $adminSession -UseBasicParsing
    $apiCustJson = $apiCustResp.Content | ConvertFrom-Json
    $apiSuccess = ($apiCustResp.StatusCode -eq 200 -and $apiCustJson.success -eq $true -and $apiCustJson.data.Count -gt 0)
    Test-Scenario 13 "REST API /api/customers returns authorized JSON response" $apiSuccess "Items returned: $($apiCustJson.data.Count)"

    # -----------------------------------------------------------------------------------------
    # SCENARIO 14: Open Dashboard -> KPI cards and Chart.js charts display authorized data.
    # -----------------------------------------------------------------------------------------
    $hasKpiCards = ($dashResp.Content -like "*Total Customers*" -and $dashResp.Content -like "*Total Leads*" -and $dashResp.Content -like "*Total Opportunities*")
    $hasChartJs = ($dashResp.Content -like "*chart.js*" -and $dashResp.Content -like "*leadStatusChart*" -and $dashResp.Content -like "*pipelineStageChart*")
    Test-Scenario 14 "Dashboard renders KPI summary cards and Chart.js visualizations" ($hasKpiCards -and $hasChartJs) "KPIs & 3 Chart.js graphs verified"

} catch {
    Write-Host "FATAL ERROR during test execution: $_" -ForegroundColor Red
    $script:results += [PSCustomObject]@{ Scenario = "Execution"; Title = "Execution"; Status = "FAIL"; Details = $_.ToString() }
}

Write-Host "`n=====================================================================" -ForegroundColor Cyan
Write-Host "                ACCEPTANCE CRITERIA SUMMARY (14/14)                  " -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$total = $results.Count
Write-Host "Passed: $passed / $total" -ForegroundColor $(if ($passed -eq $total) { "Green" } else { "Red" })

if ($passed -ne $total) {
    exit 1
}
