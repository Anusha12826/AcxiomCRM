$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "       ACXIOMCRM - PHASE 12 REPORTS MODULE TESTS    " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5175"
$results = @()

function Test-Assert($name, $condition, $details = "") {
    if ($condition) {
        Write-Host " [PASS] $name" -ForegroundColor Green
        $script:results += [PSCustomObject]@{ Test = $name; Status = "PASS"; Details = $details }
    } else {
        Write-Host " [FAIL] $name - $details" -ForegroundColor Red
        $script:results += [PSCustomObject]@{ Test = $name; Status = "FAIL"; Details = $details }
    }
}

try {
    # 1. Test Unauthorized Access Protection
    Write-Host "`n--- Testing Anonymous Access Protection ---" -ForegroundColor Yellow
    $unauthProtected = $false
    try {
        $unauthResp = Invoke-WebRequest -Uri "$baseUrl/Reports" -Method Get -MaximumRedirection 0 -UseBasicParsing
        $unauthProtected = ($unauthResp.StatusCode -eq 401 -or $unauthResp.StatusCode -eq 302)
    } catch {
        $msg = "$_"
        if ($msg -like "*redirection*" -or $msg -like "*401*" -or $msg -like "*302*" -or $msg -like "*Login*") {
            $unauthProtected = $true
        }
    }
    Test-Assert "Unauthenticated access to /Reports blocked/redirected" $unauthProtected "Protected"

    # 2. Authenticate as Admin
    Write-Host "`n--- Authenticating as Admin ---" -ForegroundColor Yellow
    $adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $adminSession -UseBasicParsing
    $tokenMatch = [regex]::Match($loginPage.Content, '__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $tokenMatch.Success) {
        $tokenMatch = [regex]::Match($loginPage.Content, 'value="([^"]+)" name="__RequestVerificationToken"')
    }
    $token = $tokenMatch.Groups[1].Value

    $loginBody = @{
        "Email" = "admin@acxiomcrm.com"
        "Password" = "Admin@12345"
        "RememberMe" = "false"
        "__RequestVerificationToken" = $token
    }
    $loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $adminSession -UseBasicParsing
    Test-Assert "Admin login successful" ($loginResp.StatusCode -eq 200 -and $adminSession.Cookies.Count -gt 0) "Session cookies: $($adminSession.Cookies.Count)"

    # 3. Test Admin Access to All 8 Reports
    Write-Host "`n--- Testing Admin Report Access (All 8 Reports) ---" -ForegroundColor Yellow
    
    # Report Catalog
    $indexResp = Invoke-WebRequest -Uri "$baseUrl/Reports" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Report Catalog (/Reports) renders" ($indexResp.StatusCode -eq 200 -and $indexResp.Content -like "*CRM Reports & Business Intelligence*") "Status: $($indexResp.StatusCode)"

    # Report 1: Customers
    $custResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Customers" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Customer Report renders" ($custResp.StatusCode -eq 200 -and $custResp.Content -like "*Customer Report*") "Status: $($custResp.StatusCode)"

    # Report 2: Leads
    $leadResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Leads" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Lead Report renders" ($leadResp.StatusCode -eq 200 -and $leadResp.Content -like "*Lead Report*") "Status: $($leadResp.StatusCode)"

    # Report 3: FollowUps
    $fllwResp = Invoke-WebRequest -Uri "$baseUrl/Reports/FollowUps" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Follow-Up Compliance Report renders" ($fllwResp.StatusCode -eq 200 -and $fllwResp.Content -like "*Follow-Up Compliance Report*") "Status: $($fllwResp.StatusCode)"

    # Report 4: Opportunities
    $oppResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Opportunities" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Opportunity Forecast Report renders" ($oppResp.StatusCode -eq 200 -and $oppResp.Content -like "*Opportunity Forecast Report*") "Status: $($oppResp.StatusCode)"

    # Report 5: Pipeline Matrix
    $pipelineResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Pipeline" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Sales Pipeline Matrix renders" ($pipelineResp.StatusCode -eq 200 -and $pipelineResp.Content -like "*Sales Pipeline Matrix*") "Status: $($pipelineResp.StatusCode)"

    # Report 6: Conversion
    $convResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Conversion" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Conversion Report renders" ($convResp.StatusCode -eq 200 -and $convResp.Content -like "*Conversion Performance*") "Status: $($convResp.StatusCode)"

    # Report 7: UserActivity (Admin permitted)
    $actResp = Invoke-WebRequest -Uri "$baseUrl/Reports/UserActivity" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "User Activity Report renders (Admin)" ($actResp.StatusCode -eq 200 -and $actResp.Content -like "*Representative Engagement Matrix*") "Status: $($actResp.StatusCode)"

    # Report 8: Audit (Admin permitted)
    $auditResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Audit" -Method Get -WebSession $adminSession -UseBasicParsing
    Test-Assert "Audit Compliance Report renders (Admin)" ($auditResp.StatusCode -eq 200 -and $auditResp.Content -like "*Audit Compliance & Security Report*") "Status: $($auditResp.StatusCode)"

    # 4. Test CSV Export Endpoints
    Write-Host "`n--- Testing Report CSV Exports ---" -ForegroundColor Yellow
    $csvCustResp = Invoke-WebRequest -Uri "$baseUrl/Reports/ExportCsv?reportType=Customers" -Method Get -WebSession $adminSession -UseBasicParsing
    $isCsvCust = ($csvCustResp.StatusCode -eq 200 -and $csvCustResp.Content -like "*Code,Name,Email,Phone,Company,City,Status,Owner,CreatedDate*")
    Test-Assert "Customer Report CSV export valid" $isCsvCust "Content length: $($csvCustResp.Content.Length)"

    $csvOppResp = Invoke-WebRequest -Uri "$baseUrl/Reports/ExportCsv?reportType=Opportunities" -Method Get -WebSession $adminSession -UseBasicParsing
    $isCsvOpp = ($csvOppResp.StatusCode -eq 200 -and $csvOppResp.Content -like "*OpportunityName,Customer/Lead,Stage,Amount*")
    Test-Assert "Opportunity Pipeline CSV export valid" $isCsvOpp "Content length: $($csvOppResp.Content.Length)"

    # 5. Test REST Reporting API: GET /api/reports/pipeline
    Write-Host "`n--- Testing REST API GET /api/reports/pipeline ---" -ForegroundColor Yellow
    $apiPipelineResp = Invoke-WebRequest -Uri "$baseUrl/api/reports/pipeline" -Method Get -WebSession $adminSession -UseBasicParsing
    $apiPipelineJson = $apiPipelineResp.Content | ConvertFrom-Json
    $hasPipelineData = ($apiPipelineJson.success -eq $true -and $apiPipelineJson.data.stageSummaries.Count -ge 5)
    Test-Assert "GET /api/reports/pipeline returns structured JSON with stage distribution" $hasPipelineData "Stage count: $($apiPipelineJson.data.stageSummaries.Count)"

    # 6. Test Role-Based Restrictions for SalesExecutive
    Write-Host "`n--- Testing Role Enforcement on Reports (SalesExecutive) ---" -ForegroundColor Yellow
    $salesSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $salesLoginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $salesSession -UseBasicParsing
    $salesTokenMatch = [regex]::Match($salesLoginPage.Content, '__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $salesTokenMatch.Success) {
        $salesTokenMatch = [regex]::Match($salesLoginPage.Content, 'value="([^"]+)" name="__RequestVerificationToken"')
    }
    $salesToken = $salesTokenMatch.Groups[1].Value

    $salesLoginBody = @{
        "Email" = "sales@acxiomcrm.com"
        "Password" = "Sales@12345"
        "RememberMe" = "false"
        "__RequestVerificationToken" = $salesToken
    }
    $salesLoginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $salesLoginBody -WebSession $salesSession -UseBasicParsing
    
    # Sales Executive should access own Customer and Pipeline report
    $salesCustResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Customers" -Method Get -WebSession $salesSession -UseBasicParsing
    Test-Assert "SalesExecutive can access scoped Customer Report" ($salesCustResp.StatusCode -eq 200) "Status: $($salesCustResp.StatusCode)"

    # Sales Executive should be DENIED User Activity and Audit reports
    $userActBlocked = $false
    try {
        $salesActResp = Invoke-WebRequest -Uri "$baseUrl/Reports/UserActivity" -Method Get -WebSession $salesSession -MaximumRedirection 0 -UseBasicParsing
        $userActBlocked = ($salesActResp.StatusCode -eq 403 -or $salesActResp.StatusCode -eq 302)
    } catch {
        $msg = "$_"
        if ($msg -like "*403*" -or $msg -like "*Forbidden*" -or $msg -like "*redirection*" -or $msg -like "*AccessDenied*") {
            $userActBlocked = $true
        }
    }
    Test-Assert "SalesExecutive blocked from User Activity Report (Forbidden/AccessDenied)" $userActBlocked "Protected"

    $auditBlocked = $false
    try {
        $salesAuditResp = Invoke-WebRequest -Uri "$baseUrl/Reports/Audit" -Method Get -WebSession $salesSession -MaximumRedirection 0 -UseBasicParsing
        $auditBlocked = ($salesAuditResp.StatusCode -eq 403 -or $salesAuditResp.StatusCode -eq 302)
    } catch {
        $msg = "$_"
        if ($msg -like "*403*" -or $msg -like "*Forbidden*" -or $msg -like "*redirection*" -or $msg -like "*AccessDenied*") {
            $auditBlocked = $true
        }
    }
    Test-Assert "SalesExecutive blocked from Audit Report (Forbidden/AccessDenied)" $auditBlocked "Protected"

} catch {
    Write-Host "FATAL ERROR during test execution: $_" -ForegroundColor Red
    $script:results += [PSCustomObject]@{ Test = "Execution"; Status = "FAIL"; Details = $_.ToString() }
}

Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host "                PHASE 12 TEST SUMMARY               " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$total = $results.Count
Write-Host "Passed: $passed / $total" -ForegroundColor $(if ($passed -eq $total) { "Green" } else { "Red" })

if ($passed -ne $total) {
    exit 1
}
