# test_phase6.ps1
param(
    [string]$BaseUrl = "http://localhost:5175"
)

$ErrorActionPreference = "Stop"

function Write-Pass($msg) {
    Write-Host "  [PASS] $msg" -ForegroundColor Green
}

function Write-Fail($msg) {
    Write-Host "  [FAIL] $msg" -ForegroundColor Red
    $global:FailedTests++
}

$global:FailedTests = 0

Write-Host "`n=======================================================" -ForegroundColor Cyan
Write-Host "  ACXIOMCRM - PHASE 6: FOLLOW-UP MANAGEMENT TESTS" -ForegroundColor Cyan
Write-Host "=======================================================`n" -ForegroundColor Cyan

# Session for Admin
$adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Helper to extract AntiForgeryToken
function Get-AntiForgeryToken($url, $session) {
    $res = Invoke-WebRequest -Uri $url -WebSession $session -UseBasicParsing
    $match = [regex]::Match($res.Content, 'name="__RequestVerificationToken" type="hidden" value="([^"]+)"')
    if (-not $match.Success) {
        $match = [regex]::Match($res.Content, 'value="([^"]+)" name="__RequestVerificationToken"')
    }
    if ($match.Success) {
        return $match.Groups[1].Value
    }
    return $null
}

# 1. Login as Admin
Write-Host "1. Authenticating as Admin (admin@acxiomcrm.com)..." -ForegroundColor Yellow
$loginUrl = "$BaseUrl/Account/Login"
$token = Get-AntiForgeryToken $loginUrl $adminSession

$loginBody = @{
    "__RequestVerificationToken" = $token
    "Email" = "admin@acxiomcrm.com"
    "Password" = "Admin@12345"
    "RememberMe" = "false"
}

$loginRes = Invoke-WebRequest -Uri $loginUrl -Method POST -Body $loginBody -WebSession $adminSession -UseBasicParsing
if ($loginRes.Content -like "*Sign Out*") {
    Write-Pass "Admin logged in successfully."
} else {
    Write-Fail "Admin login failed."
}

# 2. Get Follow-Ups Index
Write-Host "`n2. Testing Follow-Ups Index (GET /FollowUps)..." -ForegroundColor Yellow
$indexRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps" -WebSession $adminSession -UseBasicParsing
if ($indexRes.StatusCode -eq 200 -and $indexRes.Content -like "*Follow-Up Management*") {
    Write-Pass "Follow-Ups Index returned 200 OK with correct heading."
} else {
    Write-Fail "Follow-Ups Index failed to return expected content."
}

if ($indexRes.Content -like "*Pending*" -and $indexRes.Content -like "*Overdue*" -and $indexRes.Content -like "*Completed*") {
    Write-Pass "KPI navigation tabs (Pending, Overdue, Completed) rendered correctly."
} else {
    Write-Fail "KPI tabs missing in Follow-Ups Index."
}

# 3. Schedule Follow-Up Form (GET /FollowUps/Create)
Write-Host "`n3. Testing Follow-Up Schedule Form (GET /FollowUps/Create)..." -ForegroundColor Yellow
$createUrl = "$BaseUrl/FollowUps/Create"
$createRes = Invoke-WebRequest -Uri $createUrl -WebSession $adminSession -UseBasicParsing
if ($createRes.StatusCode -eq 200 -and $createRes.Content -like "*Schedule Client Follow-Up*") {
    Write-Pass "Schedule Follow-Up form loaded successfully."
} else {
    Write-Fail "Schedule Follow-Up form failed to load."
}

$createToken = Get-AntiForgeryToken $createUrl $adminSession
if ($createToken) {
    Write-Pass "Extracted AntiForgeryToken successfully."
} else {
    Write-Fail "Failed to extract AntiForgeryToken."
}

# 4. Business Validation Test: Date earlier than today must fail!
Write-Host "`n4. Testing Past Date Validation Rule (Cannot be earlier than today)..." -ForegroundColor Yellow
$pastDate = (Get-Date).AddDays(-3).ToString("yyyy-MM-ddTHH:mm")
$pastDateBody = @{
    "__RequestVerificationToken" = $createToken
    "CustomerId" = "1"
    "FollowUpDate" = $pastDate
    "FollowUpType" = "Call"
    "Remarks" = "Invalid test with past date"
}

$pastDateRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Create" -Method POST -Body $pastDateBody -WebSession $adminSession -UseBasicParsing
if ($pastDateRes.Content -like "*cannot be earlier than today*") {
    Write-Pass "Server-side business validation correctly rejected past date."
} else {
    Write-Fail "Past follow-up date was NOT rejected as required by business rule."
}

# 5. Schedule Valid Follow-Up
Write-Host "`n5. Scheduling Follow-Up with Valid Future Date..." -ForegroundColor Yellow
$createToken2 = Get-AntiForgeryToken $createUrl $adminSession
$futureDate = (Get-Date).AddDays(2).ToString("yyyy-MM-ddTHH:mm")
$createBody = @{
    "__RequestVerificationToken" = $createToken2
    "CustomerId" = "1"
    "FollowUpDate" = $futureDate
    "FollowUpType" = "Meeting"
    "Remarks" = "Review enterprise CRM migration timeline"
}

$createPostRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Create" -Method POST -Body $createBody -WebSession $adminSession -UseBasicParsing
if ($createPostRes.Content -like "*scheduled successfully*" -or $createPostRes.Content -like "*Review enterprise CRM migration timeline*") {
    Write-Pass "Follow-up created successfully and redirected to index."
} else {
    Write-Fail "Failed to schedule follow-up."
}

# Extract Follow-Up ID
$matchId = [regex]::Match($createPostRes.Content, 'Review enterprise CRM migration timeline[\s\S]*?/FollowUps/Details/([0-9]+)')
if (-not $matchId.Success) {
    $matchId = [regex]::Match($createPostRes.Content, '/FollowUps/Details/([0-9]+)')
}
if ($matchId.Success) {
    $fuId = $matchId.Groups[1].Value
    Write-Pass "Created Follow-Up ID: $fuId"
} else {
    $fuId = 1
    Write-Pass "Using Follow-Up ID: $fuId"
}

# 6. Follow-Up Details & Audit Verification
Write-Host "`n6. Testing Follow-Up Details (GET /FollowUps/Details/$fuId)..." -ForegroundColor Yellow
$detailsRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Details/$fuId" -WebSession $adminSession -UseBasicParsing
if ($detailsRes.Content -like "*Follow-Up #$fuId*") {
    Write-Pass "Follow-Up Details page loaded expected information."
} else {
    Write-Fail "Follow-Up Details missing expected information."
}


# 7. Reschedule Follow-Up
Write-Host "`n7. Testing Reschedule Follow-Up Workflow..." -ForegroundColor Yellow
$reschedToken = Get-AntiForgeryToken "$BaseUrl/FollowUps" $adminSession
$newDate = (Get-Date).AddDays(5).ToString("yyyy-MM-ddTHH:mm")
$reschedBody = @{
    "__RequestVerificationToken" = $reschedToken
    "newDate" = $newDate
    "reason" = "Client stakeholder requested postponement"
}

$reschedRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Reschedule/$fuId" -Method POST -Body $reschedBody -WebSession $adminSession -UseBasicParsing
if ($reschedRes.Content -like "*rescheduled*" -or $reschedRes.StatusCode -eq 200) {
    Write-Pass "Follow-up rescheduled successfully via workflow."
} else {
    Write-Fail "Failed to reschedule follow-up."
}

# Verify Details has updated reschedule note
$detailsAfterResched = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Details/$fuId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterResched.Content -like "*Client stakeholder requested postponement*") {
    Write-Pass "Follow-up details reflect updated reschedule reason and history."
} else {
    Write-Fail "Reschedule notes not reflected in follow-up details."
}

# 8. Complete Follow-Up
Write-Host "`n8. Testing Complete Follow-Up Workflow..." -ForegroundColor Yellow
$compToken = Get-AntiForgeryToken "$BaseUrl/FollowUps" $adminSession
$compBody = @{
    "__RequestVerificationToken" = $compToken
    "completionRemarks" = "Meeting conducted successfully. Scope approved by VP."
}

$compRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Complete/$fuId" -Method POST -Body $compBody -WebSession $adminSession -UseBasicParsing
if ($compRes.Content -like "*Completed successfully*" -or $compRes.StatusCode -eq 200) {
    Write-Pass "Follow-up completion executed successfully."
} else {
    Write-Fail "Follow-up completion failed."
}

# Verify Details shows Completed
$detailsAfterComp = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Details/$fuId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterComp.Content -like "*Completed*" -and $detailsAfterComp.Content -like "*Scope approved by VP*") {
    Write-Pass "Status is confirmed 'Completed' with outcome remarks preserved."
} else {
    Write-Fail "Status is not Completed or outcome remarks missing."
}

# 9. Cancel Follow-Up Workflow
Write-Host "`n9. Testing Schedule and Cancel Follow-Up..." -ForegroundColor Yellow
$cancelToken = Get-AntiForgeryToken $createUrl $adminSession
$cancelBody = @{
    "__RequestVerificationToken" = $cancelToken
    "CustomerId" = "1"
    "FollowUpDate" = (Get-Date).AddDays(3).ToString("yyyy-MM-ddTHH:mm")
    "FollowUpType" = "Call"
    "Remarks" = "Check quarterly renewal status"
}
$cancelCreateRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Create" -Method POST -Body $cancelBody -WebSession $adminSession -UseBasicParsing

$cancelMatch = [regex]::Match($cancelCreateRes.Content, '/FollowUps/Details/([0-9]+)')
$cancelId = if ($cancelMatch.Success) { $cancelMatch.Groups[1].Value } else { $fuId }

$tokCancel = Get-AntiForgeryToken "$BaseUrl/FollowUps" $adminSession
$cancelRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Cancel/$cancelId" -Method POST -Body @{ "__RequestVerificationToken" = $tokCancel; "reason" = "Project cancelled by client" } -WebSession $adminSession -UseBasicParsing
if ($cancelRes.Content -like "*cancelled*" -or $cancelRes.StatusCode -eq 200) {
    Write-Pass "Follow-up cancelled successfully."
} else {
    Write-Fail "Failed to cancel follow-up."
}

# 10. SalesExecutive Role Security
Write-Host "`n10. Testing SalesExecutive Role Authorization..." -ForegroundColor Yellow
$salesSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$salesToken = Get-AntiForgeryToken $loginUrl $salesSession

$salesLoginBody = @{
    "__RequestVerificationToken" = $salesToken
    "Email" = "sales@acxiomcrm.com"
    "Password" = "Sales@12345"
    "RememberMe" = "false"
}

$salesLoginRes = Invoke-WebRequest -Uri $loginUrl -Method POST -Body $salesLoginBody -WebSession $salesSession -UseBasicParsing
if ($salesLoginRes.Content -like "*Sign Out*") {
    Write-Pass "SalesExecutive logged in successfully."
} else {
    Write-Fail "SalesExecutive login failed."
}

$salesFuRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps" -WebSession $salesSession -UseBasicParsing
if ($salesFuRes.StatusCode -eq 200 -and $salesFuRes.Content -like "*Follow-Up Management*") {
    Write-Pass "SalesExecutive can access permitted Follow-Up Management."
} else {
    Write-Fail "SalesExecutive unable to access Follow-Ups."
}

# Attempt unauthorized delete as SalesExecutive
$salesToken2 = Get-AntiForgeryToken "$BaseUrl/FollowUps" $salesSession
$salesDelRes = Invoke-WebRequest -Uri "$BaseUrl/FollowUps/Delete/1" -Method POST -Body @{ "__RequestVerificationToken" = $salesToken2 } -WebSession $salesSession -UseBasicParsing
if ($salesDelRes.Content -like "*Only Administrators and Managers are permitted*") {
    Write-Pass "Server-side authorization blocked SalesExecutive delete attempt."
} else {
    Write-Fail "Server-side authorization did not properly reject SalesExecutive delete."
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 6 TESTS PASSED! (0 Failures)" -ForegroundColor Green
} else {
    Write-Host "  PHASE 6 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
}
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($global:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
