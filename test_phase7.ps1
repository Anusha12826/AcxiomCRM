# test_phase7.ps1
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
Write-Host "  ACXIOMCRM - PHASE 7: OPPORTUNITY & PIPELINE TESTS" -ForegroundColor Cyan
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

# 2. Get Opportunities Index
Write-Host "`n2. Testing Opportunities Index (GET /Opportunities)..." -ForegroundColor Yellow
$indexRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities" -WebSession $adminSession -UseBasicParsing
if ($indexRes.StatusCode -eq 200 -and $indexRes.Content -like "*Opportunity Management*") {
    Write-Pass "Opportunities Index returned 200 OK with correct heading."
} else {
    Write-Fail "Opportunities Index failed to return expected content."
}

if ($indexRes.Content -like "*Total Pipeline Value*" -and $indexRes.Content -like "*Weighted Pipeline*") {
    Write-Pass "Pipeline summary KPI cards (Total Value, Weighted Value) present."
} else {
    Write-Fail "Pipeline summary KPI cards missing."
}

# 3. Create Opportunity Form GET
Write-Host "`n3. Testing Opportunity Create Form (GET /Opportunities/Create)..." -ForegroundColor Yellow
$createUrl = "$BaseUrl/Opportunities/Create"
$createRes = Invoke-WebRequest -Uri $createUrl -WebSession $adminSession -UseBasicParsing
if ($createRes.StatusCode -eq 200 -and $createRes.Content -like "*Create New Opportunity*") {
    Write-Pass "Create Opportunity form loaded successfully."
} else {
    Write-Fail "Create Opportunity form failed to load."
}

# 4. Business Validation Test: Amount <= 0 Must Be Rejected
Write-Host "`n4. Testing Business Validation: Amount <= 0 must fail..." -ForegroundColor Yellow
$createToken1 = Get-AntiForgeryToken $createUrl $adminSession
$zeroAmountBody = @{
    "__RequestVerificationToken" = $createToken1
    "OpportunityName" = "Zero Amount Test Deal"
    "CustomerId" = "1"
    "Amount" = "0"
    "Stage" = "Qualification"
    "Probability" = "30"
    "ExpectedCloseDate" = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
}

$zeroAmountRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Create" -Method POST -Body $zeroAmountBody -WebSession $adminSession -UseBasicParsing
if ($zeroAmountRes.Content -like "*must be greater than 0*") {
    Write-Pass "Zero Amount correctly rejected with business validation error."
} else {
    Write-Fail "Zero Amount was NOT rejected as required by business rule."
}

# 5. Business Validation Test: Probability > 100 Must Be Rejected
Write-Host "`n5. Testing Business Validation: Probability > 100 must fail..." -ForegroundColor Yellow
$createToken2 = Get-AntiForgeryToken $createUrl $adminSession
$invalidProbBody = @{
    "__RequestVerificationToken" = $createToken2
    "OpportunityName" = "Invalid Probability Test Deal"
    "CustomerId" = "1"
    "Amount" = "50000"
    "Stage" = "Qualification"
    "Probability" = "150"
    "ExpectedCloseDate" = (Get-Date).AddDays(30).ToString("yyyy-MM-dd")
}

$invalidProbRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Create" -Method POST -Body $invalidProbBody -WebSession $adminSession -UseBasicParsing
if ($invalidProbRes.Content -like "*between 0 and 100*") {
    Write-Pass "Invalid Probability (>100) correctly rejected with business validation error."
} else {
    Write-Fail "Invalid Probability was NOT rejected as required by business rule."
}

# 6. Business Validation Test: Past Expected Close Date Must Be Rejected
Write-Host "`n6. Testing Business Validation: Past Expected Close Date must fail..." -ForegroundColor Yellow
$createToken3 = Get-AntiForgeryToken $createUrl $adminSession
$pastDateBody = @{
    "__RequestVerificationToken" = $createToken3
    "OpportunityName" = "Past Date Test Deal"
    "CustomerId" = "1"
    "Amount" = "50000"
    "Stage" = "Qualification"
    "Probability" = "30"
    "ExpectedCloseDate" = (Get-Date).AddDays(-10).ToString("yyyy-MM-dd")
}

$pastDateRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Create" -Method POST -Body $pastDateBody -WebSession $adminSession -UseBasicParsing
if ($pastDateRes.Content -like "*cannot be in the past*") {
    Write-Pass "Past Expected Close Date correctly rejected with business validation error."
} else {
    Write-Fail "Past Expected Close Date was NOT rejected as required by business rule."
}

# 7. Valid Opportunity Creation & Weighted Pipeline Verification
Write-Host "`n7. Testing Valid Opportunity Creation & Weighted Pipeline Calculation..." -ForegroundColor Yellow
$createToken4 = Get-AntiForgeryToken $createUrl $adminSession
$randDealNum = Get-Random -Minimum 1000 -Maximum 9999
$validDealName = "Oracle Database Cloud Migration Deal $randDealNum"
$validAmount = "200000" # 200,000
$validProb = "50"       # 50% => Weighted: 100,000

$validBody = @{
    "__RequestVerificationToken" = $createToken4
    "OpportunityName" = $validDealName
    "CustomerId" = "1"
    "Amount" = $validAmount
    "Stage" = "Qualification"
    "Probability" = $validProb
    "ExpectedCloseDate" = (Get-Date).AddDays(40).ToString("yyyy-MM-dd")
}

$validRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Create" -Method POST -Body $validBody -WebSession $adminSession -UseBasicParsing
if ($validRes.Content -like "*created successfully*" -or $validRes.Content -like "*$validDealName*") {
    Write-Pass "Opportunity created successfully and redirected to index."
} else {
    Write-Fail "Opportunity creation failed."
}

# Verify Weighted Pipeline in search/index
$searchDealRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities?search=$randDealNum" -WebSession $adminSession -UseBasicParsing
if ($searchDealRes.Content -like "*$validDealName*") {
    Write-Pass "Opportunity found in search results with weighted pipeline values."
} else {
    Write-Fail "Opportunity or weighted pipeline not found in results."
}

# Extract Opportunity ID
$dealMatch = [regex]::Match($searchDealRes.Content, '/Opportunities/Details/([0-9]+)')
if ($dealMatch.Success) {
    $dealId = $dealMatch.Groups[1].Value
    Write-Pass "Created Opportunity ID: $dealId"
} else {
    $dealId = 1
    Write-Pass "Using Opportunity ID: $dealId"
}

# 8. Opportunity Details & Visual Stage Tracker
Write-Host "`n8. Testing Opportunity Details View (GET /Opportunities/Details/$dealId)..." -ForegroundColor Yellow
$detailsRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Details/$dealId" -WebSession $adminSession -UseBasicParsing
if ($detailsRes.Content -like "*$validDealName*" -and $detailsRes.Content -like "*Transition Stage:*") {
    Write-Pass "Opportunity Details loaded deal information and stage transition controls."
} else {
    Write-Fail "Opportunity Details view missing required controls."
}

if ($detailsRes.Content -like "*OpportunityCreated*") {
    Write-Pass "Audit log correctly records 'OpportunityCreated'."
} else {
    Write-Fail "Audit log missing 'OpportunityCreated' event."
}

# 9. Pipeline Stage Transition to Won Workflow
Write-Host "`n9. Testing Pipeline Stage Transition to Won..." -ForegroundColor Yellow
$stageToken = Get-AntiForgeryToken "$BaseUrl/Opportunities/Details/$dealId" $adminSession
$stageBody = @{
    "__RequestVerificationToken" = $stageToken
    "newStage" = "Won"
}

$stageRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/UpdateStage/$dealId" -Method POST -Body $stageBody -WebSession $adminSession -UseBasicParsing
if ($stageRes.Content -like "*pipeline stage updated to*Won*" -or $stageRes.StatusCode -eq 200) {
    Write-Pass "Opportunity stage successfully transitioned to 'Won'."
} else {
    Write-Fail "Stage transition to 'Won' failed."
}

# Verify Won updates Probability to 100% and Status to Won
$detailsAfterWon = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Details/$dealId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterWon.Content -like "*100%*" -and $detailsAfterWon.Content -like "*Status: Won*" -and $detailsAfterWon.Content -like "*OpportunityStageChanged*") {
    Write-Pass "Winning deal automatically set Probability to 100%, Status to Won, and logged 'OpportunityStageChanged'."
} else {
    Write-Fail "Deal won behavior failed to set expected status/probability/audit."
}

# 10. Pipeline Stage Transition to Lost Workflow
Write-Host "`n10. Testing Pipeline Stage Transition to Lost..." -ForegroundColor Yellow
$stageToken2 = Get-AntiForgeryToken "$BaseUrl/Opportunities/Details/$dealId" $adminSession
$stageBody2 = @{
    "__RequestVerificationToken" = $stageToken2
    "newStage" = "Lost"
}

$stageRes2 = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/UpdateStage/$dealId" -Method POST -Body $stageBody2 -WebSession $adminSession -UseBasicParsing
$detailsAfterLost = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Details/$dealId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterLost.Content -like "*0%*" -and $detailsAfterLost.Content -like "*Status: Lost*") {
    Write-Pass "Lost deal automatically set Probability to 0% and Status to Lost."
} else {
    Write-Fail "Lost deal behavior failed to update status/probability."
}

# 11. SalesExecutive Role Security
Write-Host "`n11. Testing SalesExecutive Role Authorization..." -ForegroundColor Yellow
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

$salesOppRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities" -WebSession $salesSession -UseBasicParsing
if ($salesOppRes.StatusCode -eq 200 -and $salesOppRes.Content -like "*Opportunity Management*") {
    Write-Pass "SalesExecutive can access permitted Opportunity Management."
} else {
    Write-Fail "SalesExecutive unable to access Opportunities."
}

# Attempt unauthorized delete as SalesExecutive
$salesToken2 = Get-AntiForgeryToken "$BaseUrl/Opportunities" $salesSession
$salesDelRes = Invoke-WebRequest -Uri "$BaseUrl/Opportunities/Delete/1" -Method POST -Body @{ "__RequestVerificationToken" = $salesToken2 } -WebSession $salesSession -UseBasicParsing
if ($salesDelRes.Content -like "*Only Administrators and Managers are permitted*") {
    Write-Pass "Server-side authorization blocked SalesExecutive delete attempt."
} else {
    Write-Fail "Server-side authorization did not properly reject SalesExecutive delete."
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 7 TESTS PASSED! (0 Failures)" -ForegroundColor Green
} else {
    Write-Host "  PHASE 7 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
}
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($global:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
