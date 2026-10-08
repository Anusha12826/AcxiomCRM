# test_phase10.ps1
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
Write-Host "  ACXIOMCRM - PHASE 10: CENTRALIZED AUDIT LOG TESTS" -ForegroundColor Cyan
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

# 2. Get Audit Log Index
Write-Host "`n2. Testing Audit Log Index (GET /Audit)..." -ForegroundColor Yellow
$auditRes = Invoke-WebRequest -Uri "$BaseUrl/Audit" -WebSession $adminSession -UseBasicParsing
if ($auditRes.StatusCode -eq 200 -and $auditRes.Content -like "*System Audit Logs*") {
    Write-Pass "Audit Log page returned 200 OK with correct heading."
} else {
    Write-Fail "Audit Log page failed to load."
}

if ($auditRes.Content -like "*Append-Only Protected*") {
    Write-Pass "Append-only compliance notice is displayed."
} else {
    Write-Fail "Append-only notice missing."
}

# 3. Verify Filter Controls
Write-Host "`n3. Testing Filter Controls on Audit Log..." -ForegroundColor Yellow
if ($auditRes.Content -like "*entityName*" -and $auditRes.Content -like "*action*" -and $auditRes.Content -like "*fromDate*") {
    Write-Pass "Module, Action, and Date range filter controls rendered properly."
} else {
    Write-Fail "Filter controls missing on Audit page."
}

# 4. Test Action Filter (e.g. CustomerCreated)
Write-Host "`n4. Testing Action Filter (action=CustomerCreated)..." -ForegroundColor Yellow
$filterActionRes = Invoke-WebRequest -Uri "$BaseUrl/Audit?action=CustomerCreated" -WebSession $adminSession -UseBasicParsing
if ($filterActionRes.Content -like "*CustomerCreated*") {
    Write-Pass "Action filter correctly returned CustomerCreated audit entries."
} else {
    Write-Fail "Action filter failed to return expected records."
}

# 5. Test Module Filter (e.g. entityName=Customer)
Write-Host "`n5. Testing Entity/Module Filter (entityName=Customer)..." -ForegroundColor Yellow
$filterEntityRes = Invoke-WebRequest -Uri "$BaseUrl/Audit?entityName=Customer" -WebSession $adminSession -UseBasicParsing
if ($filterEntityRes.Content -like "*Customer*") {
    Write-Pass "Entity filter correctly returned Customer records."
} else {
    Write-Fail "Entity filter failed to return expected records."
}

# 6. Test Audit Details View
Write-Host "`n6. Testing Audit Log Details (GET /Audit/Details/1)..." -ForegroundColor Yellow
# Extract first available audit log ID from page
$logMatch = [regex]::Match($auditRes.Content, '/Audit/Details/([0-9]+)')
$logId = if ($logMatch.Success) { $logMatch.Groups[1].Value } else { "1" }

$detailsRes = Invoke-WebRequest -Uri "$BaseUrl/Audit/Details/$logId" -WebSession $adminSession -UseBasicParsing
if ($detailsRes.StatusCode -eq 200 -and $detailsRes.Content -like "*Audit Log Record #$logId*") {
    Write-Pass "Audit Details page loaded record #$logId successfully."
} else {
    Write-Fail "Audit Details page failed to load."
}

if ($detailsRes.Content -like "*Pre-Change State*" -and $detailsRes.Content -like "*Post-Change State*") {
    Write-Pass "Audit Details renders Old Value vs New Value comparison blocks."
} else {
    Write-Fail "State comparison blocks missing in Audit Details."
}

# 7. Role Security: SalesExecutive Blocked from Audit Logs
Write-Host "`n7. Testing Role Security: SalesExecutive blocked from /Audit..." -ForegroundColor Yellow
$salesSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$salesToken = Get-AntiForgeryToken $loginUrl $salesSession

$salesLoginBody = @{
    "__RequestVerificationToken" = $salesToken
    "Email" = "sales@acxiomcrm.com"
    "Password" = "Sales@12345"
    "RememberMe" = "false"
}

Invoke-WebRequest -Uri $loginUrl -Method POST -Body $salesLoginBody -WebSession $salesSession -UseBasicParsing | Out-Null

$salesAuditRes = Invoke-WebRequest -Uri "$BaseUrl/Audit" -WebSession $salesSession -UseBasicParsing
if ($salesAuditRes.Content -like "*Access Denied*" -or $salesAuditRes.BaseResponse.ResponseUri -like "*AccessDenied*") {
    Write-Pass "SalesExecutive access to /Audit blocked and redirected to Access Denied."
} else {
    Write-Fail "SalesExecutive was improperly permitted to access Audit Logs!"
}

# 8. Role Security: Manager Blocked from Audit Logs (Admin-only privilege)
Write-Host "`n8. Testing Role Security: Manager blocked from /Audit..." -ForegroundColor Yellow
$mgrSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$mgrToken = Get-AntiForgeryToken $loginUrl $mgrSession

$mgrLoginBody = @{
    "__RequestVerificationToken" = $mgrToken
    "Email" = "manager@acxiomcrm.com"
    "Password" = "Manager@12345"
    "RememberMe" = "false"
}

Invoke-WebRequest -Uri $loginUrl -Method POST -Body $mgrLoginBody -WebSession $mgrSession -UseBasicParsing | Out-Null

$mgrAuditRes = Invoke-WebRequest -Uri "$BaseUrl/Audit" -WebSession $mgrSession -UseBasicParsing
if ($mgrAuditRes.Content -like "*Access Denied*" -or $mgrAuditRes.BaseResponse.ResponseUri -like "*AccessDenied*") {
    Write-Pass "Manager access to /Audit blocked and redirected to Access Denied."
} else {
    Write-Fail "Manager was improperly permitted to access Audit Logs!"
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 10 TESTS PASSED! (0 Failures)" -ForegroundColor Green
} else {
    Write-Host "  PHASE 10 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
}
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($global:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
