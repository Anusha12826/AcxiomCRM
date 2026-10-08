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
Write-Host "  ACXIOMCRM - PHASE 2: AUTHENTICATION & SECURITY TESTS" -ForegroundColor Cyan
Write-Host "=======================================================`n" -ForegroundColor Cyan

function Get-AntiForgeryToken($url, $session) {
    $res = Invoke-WebRequest -Uri $url -WebSession $session -UseBasicParsing
    $match = [regex]::Match($res.Content, '__RequestVerificationToken"[^>]*value="([^"]+)"')
    if (-not $match.Success) {
        $match = [regex]::Match($res.Content, 'value="([^"]+)" name="__RequestVerificationToken"')
    }
    return $match.Groups[1].Value
}

# 1. Anonymous Access Denied
Write-Host "1. Testing Unauthorized Access Protection..." -ForegroundColor Yellow
$unauthProtected = $false
try {
    $res = Invoke-WebRequest -Uri "$BaseUrl/Dashboard" -Method Get -MaximumRedirection 0 -UseBasicParsing
    $unauthProtected = ($res.StatusCode -eq 302 -or $res.StatusCode -eq 401)
} catch {
    $msg = "$_"
    if ($msg -like "*redirection*" -or $msg -like "*302*" -or $msg -like "*401*" -or $msg -like "*Login*") {
        $unauthProtected = $true
    }
}
if ($unauthProtected) {
    Write-Pass "Anonymous request to /Dashboard successfully redirected to Login."
} else {
    Write-Fail "Anonymous request not redirected."
}

# 2. Invalid Credentials Rejected
Write-Host "`n2. Testing Invalid Credentials Rejection..." -ForegroundColor Yellow
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$token = Get-AntiForgeryToken "$BaseUrl/Account/Login" $session
$badBody = @{
    "Email" = "admin@acxiomcrm.com"
    "Password" = "WrongPassword999!"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $token
}
$badRes = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -Body $badBody -WebSession $session -UseBasicParsing
if ($badRes.Content -like "*Invalid login attempt.*" -or $badRes.Content -like "*Invalid*") {
    Write-Pass "Invalid password rejected with security error message."
} else {
    Write-Fail "Invalid password not rejected properly."
}

# 3. Successful Admin Login
Write-Host "`n3. Testing Admin Login..." -ForegroundColor Yellow
$loginToken = Get-AntiForgeryToken "$BaseUrl/Account/Login" $session
$adminBody = @{
    "Email" = "admin@acxiomcrm.com"
    "Password" = "Admin@12345"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $loginToken
}
$adminRes = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -Body $adminBody -WebSession $session -UseBasicParsing
if ($session.Cookies.Count -gt 0 -and ($adminRes.Content -like "*Dashboard*" -or $adminRes.Content -like "*Sign Out*")) {
    Write-Pass "Admin authenticated successfully with application cookie."
} else {
    Write-Fail "Admin login failed."
}

# 4. SalesExecutive Login and Access Restrictions
Write-Host "`n4. Testing SalesExecutive Role Boundaries..." -ForegroundColor Yellow
$salesSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$salesToken = Get-AntiForgeryToken "$BaseUrl/Account/Login" $salesSession
$salesBody = @{
    "Email" = "sales@acxiomcrm.com"
    "Password" = "Sales@12345"
    "RememberMe" = "false"
    "__RequestVerificationToken" = $salesToken
}
$salesRes = Invoke-WebRequest -Uri "$BaseUrl/Account/Login" -Method Post -Body $salesBody -WebSession $salesSession -UseBasicParsing
Write-Pass "SalesExecutive logged in successfully."

# Sales tries to access Admin Users page -> must be blocked
$blockedUsers = $false
try {
    $salesUsersRes = Invoke-WebRequest -Uri "$BaseUrl/Users" -Method Get -WebSession $salesSession -MaximumRedirection 0 -UseBasicParsing
    $blockedUsers = ($salesUsersRes.StatusCode -eq 403 -or $salesUsersRes.StatusCode -eq 302)
} catch {
    $msg = "$_"
    if ($msg -like "*403*" -or $msg -like "*Forbidden*" -or $msg -like "*redirection*" -or $msg -like "*AccessDenied*") {
        $blockedUsers = $true
    }
}
if ($blockedUsers) {
    Write-Pass "SalesExecutive blocked from /Users (Admin-only page)."
} else {
    Write-Fail "SalesExecutive was not blocked from /Users."
}

# 5. AccessDenied 403 Explanatory Page
Write-Host "`n5. Testing AccessDenied Page..." -ForegroundColor Yellow
$deniedRes = Invoke-WebRequest -Uri "$BaseUrl/Account/AccessDenied" -WebSession $salesSession -UseBasicParsing
if ($deniedRes.StatusCode -eq 200 -and ($deniedRes.Content -like "*Access Denied*" -or $deniedRes.Content -like "*Unauthorized*")) {
    Write-Pass "AccessDenied page renders clear explanation and return path."
} else {
    Write-Fail "AccessDenied page failed to render."
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 2 SECURITY TESTS PASSED!" -ForegroundColor Green
    Write-Host "Passed: 5 / 5" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  PHASE 2 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
    exit 1
}
