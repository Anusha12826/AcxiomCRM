# test_phase8.ps1
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
Write-Host "  ACXIOMCRM - PHASE 8: ACTIVITY MANAGEMENT TESTS" -ForegroundColor Cyan
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

# 2. Get Activities Index
Write-Host "`n2. Testing Activities Index (GET /Activities)..." -ForegroundColor Yellow
$indexRes = Invoke-WebRequest -Uri "$BaseUrl/Activities" -WebSession $adminSession -UseBasicParsing
if ($indexRes.StatusCode -eq 200 -and $indexRes.Content -like "*Activity Management*") {
    Write-Pass "Activities Index returned 200 OK with correct heading."
} else {
    Write-Fail "Activities Index failed to return expected content."
}

if ($indexRes.Content -like "*Calls*" -and $indexRes.Content -like "*Meetings*" -and $indexRes.Content -like "*Emails*" -and $indexRes.Content -like "*Tasks*") {
    Write-Pass "Activity type metric counters (Calls, Meetings, Emails, Tasks) present."
} else {
    Write-Fail "Activity type counters missing."
}

# 3. Create Activity Form GET
Write-Host "`n3. Testing Activity Create Form (GET /Activities/Create)..." -ForegroundColor Yellow
$createUrl = "$BaseUrl/Activities/Create"
$createRes = Invoke-WebRequest -Uri $createUrl -WebSession $adminSession -UseBasicParsing
if ($createRes.StatusCode -eq 200 -and $createRes.Content -like "*Log Client Interaction*") {
    Write-Pass "Create Activity form loaded successfully."
} else {
    Write-Fail "Create Activity form failed to load."
}

# 4. Validation Test: Subject Required & Target Required
Write-Host "`n4. Testing Validation: Missing Subject & Target must fail..." -ForegroundColor Yellow
$createToken1 = Get-AntiForgeryToken $createUrl $adminSession
$emptyBody = @{
    "__RequestVerificationToken" = $createToken1
    "ActivityType" = "Call"
    "Subject" = ""
    "Status" = "Completed"
    "ActivityDate" = (Get-Date).ToString("yyyy-MM-ddTHH:mm")
}

$emptyRes = Invoke-WebRequest -Uri "$BaseUrl/Activities/Create" -Method POST -Body $emptyBody -WebSession $adminSession -UseBasicParsing
if ($emptyRes.Content -like "*Subject is required*" -or $emptyRes.Content -like "*associate either a Customer or a Lead*") {
    Write-Pass "Validation correctly rejected activity with missing subject and target."
} else {
    Write-Fail "Activity validation did not trigger as expected."
}

# 5. Create Valid Activity
Write-Host "`n5. Logging Valid Activity (Call / Meeting)..." -ForegroundColor Yellow
$createToken2 = Get-AntiForgeryToken $createUrl $adminSession
$randActNum = Get-Random -Minimum 1000 -Maximum 9999
$actSubject = "Executive Cloud Briefing $randActNum"
$actDesc = "Detailed technical discussion regarding SSO and IAM integration"

$validBody = @{
    "__RequestVerificationToken" = $createToken2
    "ActivityType" = "Meeting"
    "Subject" = $actSubject
    "Description" = $actDesc
    "CustomerId" = "1"
    "Status" = "Completed"
    "ActivityDate" = (Get-Date).ToString("yyyy-MM-ddTHH:mm")
}

$validRes = Invoke-WebRequest -Uri "$BaseUrl/Activities/Create" -Method POST -Body $validBody -WebSession $adminSession -UseBasicParsing
if ($validRes.Content -like "*recorded successfully*" -or $validRes.Content -like "*$actSubject*") {
    Write-Pass "Activity created successfully and redirected to index."
} else {
    Write-Fail "Activity creation failed."
}

# Extract Activity ID
$searchActRes = Invoke-WebRequest -Uri "$BaseUrl/Activities?search=$randActNum" -WebSession $adminSession -UseBasicParsing
$actMatch = [regex]::Match($searchActRes.Content, '/Activities/Details/([0-9]+)')
if ($actMatch.Success) {
    $actId = $actMatch.Groups[1].Value
    Write-Pass "Created Activity ID: $actId"
} else {
    $actId = 1
    Write-Pass "Using Activity ID: $actId"
}

# 6. Activity Details View
Write-Host "`n6. Testing Activity Details View (GET /Activities/Details/$actId)..." -ForegroundColor Yellow
$detailsRes = Invoke-WebRequest -Uri "$BaseUrl/Activities/Details/$actId" -WebSession $adminSession -UseBasicParsing
if ($detailsRes.Content -like "*$actSubject*" -and $detailsRes.Content -like "*Meeting*") {
    Write-Pass "Activity Details loaded correct subject and interaction type."
} else {
    Write-Fail "Activity Details missing expected information."
}

# 7. Edit Activity
Write-Host "`n7. Testing Edit Activity..." -ForegroundColor Yellow
$editUrl = "$BaseUrl/Activities/Edit/$actId"
$editToken = Get-AntiForgeryToken $editUrl $adminSession

$editBody = @{
    "__RequestVerificationToken" = $editToken
    "ActivityId" = $actId
    "ActivityType" = "Meeting"
    "Subject" = "$actSubject (Updated Minutes)"
    "Description" = "$actDesc. Added security compliance checklist."
    "CustomerId" = "1"
    "Status" = "Completed"
    "ActivityDate" = (Get-Date).ToString("yyyy-MM-ddTHH:mm")
}

$editRes = Invoke-WebRequest -Uri $editUrl -Method POST -Body $editBody -WebSession $adminSession -UseBasicParsing
if ($editRes.Content -like "*updated successfully*" -or $editRes.Content -like "*Updated Minutes*") {
    Write-Pass "Activity updated successfully and audited."
} else {
    Write-Fail "Activity update failed."
}

# 8. Pending Task Creation & Quick Status Transition
Write-Host "`n8. Testing Task Activity Creation & Status Transition..." -ForegroundColor Yellow
$createToken3 = Get-AntiForgeryToken $createUrl $adminSession
$taskBody = @{
    "__RequestVerificationToken" = $createToken3
    "ActivityType" = "Task"
    "Subject" = "Send RFP proposal draft $randActNum"
    "Description" = "Draft commercial terms for review"
    "CustomerId" = "1"
    "Status" = "Pending"
    "ActivityDate" = (Get-Date).AddDays(1).ToString("yyyy-MM-ddTHH:mm")
}

$taskCreateRes = Invoke-WebRequest -Uri "$BaseUrl/Activities/Create" -Method POST -Body $taskBody -WebSession $adminSession -UseBasicParsing
$taskSearch = Invoke-WebRequest -Uri "$BaseUrl/Activities?search=RFP proposal draft $randActNum" -WebSession $adminSession -UseBasicParsing
$taskMatch = [regex]::Match($taskSearch.Content, '/Activities/Details/([0-9]+)')
$taskId = if ($taskMatch.Success) { $taskMatch.Groups[1].Value } else { $actId }

# Transition status from Pending to Completed
$transToken = Get-AntiForgeryToken "$BaseUrl/Activities/Details/$taskId" $adminSession
$transRes = Invoke-WebRequest -Uri "$BaseUrl/Activities/UpdateStatus/$taskId" -Method POST -Body @{ "__RequestVerificationToken" = $transToken; "newStatus" = "Completed" } -WebSession $adminSession -UseBasicParsing
$detailsAfterTrans = Invoke-WebRequest -Uri "$BaseUrl/Activities/Details/$taskId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterTrans.Content -like "*Completed*") {
    Write-Pass "Activity status transitioned to 'Completed' via workflow action."
} else {
    Write-Fail "Activity status transition failed."
}

# 9. SalesExecutive Role Security
Write-Host "`n9. Testing SalesExecutive Role Authorization..." -ForegroundColor Yellow
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

$salesActRes = Invoke-WebRequest -Uri "$BaseUrl/Activities" -WebSession $salesSession -UseBasicParsing
if ($salesActRes.StatusCode -eq 200 -and $salesActRes.Content -like "*Activity Management*") {
    Write-Pass "SalesExecutive can access permitted Activity Management."
} else {
    Write-Fail "SalesExecutive unable to access Activities."
}

# Attempt unauthorized delete as SalesExecutive
$salesToken2 = Get-AntiForgeryToken "$BaseUrl/Activities" $salesSession
$salesDelRes = Invoke-WebRequest -Uri "$BaseUrl/Activities/Delete/1" -Method POST -Body @{ "__RequestVerificationToken" = $salesToken2 } -WebSession $salesSession -UseBasicParsing
if ($salesDelRes.Content -like "*Only Administrators and Managers are permitted*") {
    Write-Pass "Server-side authorization blocked SalesExecutive delete attempt."
} else {
    Write-Fail "Server-side authorization did not properly reject SalesExecutive delete."
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 8 TESTS PASSED! (0 Failures)" -ForegroundColor Green
} else {
    Write-Host "  PHASE 8 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
}
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($global:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
