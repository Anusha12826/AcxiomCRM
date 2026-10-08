# test_phase5.ps1
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
Write-Host "  ACXIOMCRM - PHASE 5: LEAD MANAGEMENT & CONVERSION" -ForegroundColor Cyan
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

# 2. Get Leads Index
Write-Host "`n2. Testing Lead Index (GET /Leads)..." -ForegroundColor Yellow
$indexRes = Invoke-WebRequest -Uri "$BaseUrl/Leads" -WebSession $adminSession -UseBasicParsing
if ($indexRes.StatusCode -eq 200 -and $indexRes.Content -like "*Lead Management*") {
    Write-Pass "Lead Index returned 200 OK with correct title."
} else {
    Write-Fail "Lead Index failed to return expected content."
}

if ($indexRes.Content -like "*Amitabh Sen*" -or $indexRes.Content -like "*Reliance Digital*") {
    Write-Pass "Seeded lead records successfully listed in table."
} else {
    Write-Fail "Seeded lead records not found in list."
}

# 3. Lead Create GET
Write-Host "`n3. Testing Lead Create Form (GET /Leads/Create)..." -ForegroundColor Yellow
$createUrl = "$BaseUrl/Leads/Create"
$createRes = Invoke-WebRequest -Uri $createUrl -WebSession $adminSession -UseBasicParsing
if ($createRes.StatusCode -eq 200 -and $createRes.Content -like "*Create New Sales Lead*") {
    Write-Pass "Create Lead form loaded with status 200 OK."
} else {
    Write-Fail "Create Lead form failed to load."
}

$createToken = Get-AntiForgeryToken $createUrl $adminSession
if ($createToken) {
    Write-Pass "Extracted AntiForgeryToken successfully."
} else {
    Write-Fail "Could not extract AntiForgeryToken from create form."
}

# 4. Lead Create POST (Valid Data)
Write-Host "`n4. Testing Lead Creation with Valid Data..." -ForegroundColor Yellow
$randNum = Get-Random -Minimum 5000 -Maximum 9999
$newLeadCode = "LEAD-$randNum"
$newLeadEmail = "lead-$randNum@cloudcorp.com"
$newLeadPhone = "+91 991$randNum"
$createBody = @{
    "__RequestVerificationToken" = $createToken
    "LeadCode" = $newLeadCode
    "LeadName" = "Meera Nambiar"
    "Email" = $newLeadEmail
    "Phone" = $newLeadPhone
    "CompanyName" = "CloudCorp Solutions"
    "Source" = "Website"
    "Priority" = "High"
    "ExpectedValue" = "650000.00"
}

$createPostRes = Invoke-WebRequest -Uri "$BaseUrl/Leads/Create" -Method POST -Body $createBody -WebSession $adminSession -UseBasicParsing
if ($createPostRes.Content -like "*created successfully*" -or $createPostRes.Content -like "*Meera Nambiar*") {
    Write-Pass "Lead created successfully and redirected to index with success message."
} else {
    Write-Fail "Lead creation failed to redirect with success message."
}

# Verify lead shows up in search
$searchRes = Invoke-WebRequest -Uri "$BaseUrl/Leads?search=$randNum" -WebSession $adminSession -UseBasicParsing
if ($searchRes.Content -like "*Meera Nambiar*" -and $searchRes.Content -like "*$newLeadEmail*") {
    Write-Pass "Newly created lead found in search results."
} else {
    Write-Fail "Newly created lead not visible in search."
}

# Extract Lead ID
$idMatch = [regex]::Match($searchRes.Content, '/Leads/Details/([0-9]+)')
if (-not $idMatch.Success) {
    Write-Fail "Could not find Lead ID in search response."
    $createdLeadId = 1
} else {
    $createdLeadId = $idMatch.Groups[1].Value
    Write-Pass "Found created Lead ID: $createdLeadId"
}

# 5. Lead Details View & Audit History
Write-Host "`n5. Testing Lead Details View..." -ForegroundColor Yellow
$detailsRes = Invoke-WebRequest -Uri "$BaseUrl/Leads/Details/$createdLeadId" -WebSession $adminSession -UseBasicParsing
if ($detailsRes.Content -like "*Meera Nambiar*" -and $detailsRes.Content -like "*CloudCorp Solutions*") {
    Write-Pass "Lead Details loaded correct profile information."
} else {
    Write-Fail "Lead Details missing expected profile info."
}

if ($detailsRes.Content -like "*Opportunities*" -and $detailsRes.Content -like "*Follow-Ups*" -and $detailsRes.Content -like "*Activities*" -and $detailsRes.Content -like "*Audit History*") {
    Write-Pass "Lead Details contains all 4 tabs (Opportunities, Follow-Ups, Activities, Audit History)."
} else {
    Write-Fail "Lead Details tabs missing."
}

if ($detailsRes.Content -like "*LeadCreated*") {
    Write-Pass "Audit log correctly recorded 'LeadCreated' event."
} else {
    Write-Fail "Audit log missing 'LeadCreated' event."
}

# 6. Lead Edit
Write-Host "`n6. Testing Lead Edit..." -ForegroundColor Yellow
$editUrl = "$BaseUrl/Leads/Edit/$createdLeadId"
$editToken = Get-AntiForgeryToken $editUrl $adminSession

$editBody = @{
    "__RequestVerificationToken" = $editToken
    "LeadId" = $createdLeadId
    "LeadCode" = $newLeadCode
    "LeadName" = "Meera Nambiar Menon"
    "Email" = $newLeadEmail
    "Phone" = $newLeadPhone
    "CompanyName" = "CloudCorp Solutions International"
    "Source" = "Referral"
    "Status" = "Contacted"
    "Priority" = "Urgent"
    "ExpectedValue" = "850000.00"
}

$editPostRes = Invoke-WebRequest -Uri $editUrl -Method POST -Body $editBody -WebSession $adminSession -UseBasicParsing
if ($editPostRes.Content -like "*Meera Nambiar Menon*" -and $editPostRes.Content -like "*LeadUpdated*") {
    Write-Pass "Lead updated successfully and 'LeadUpdated' logged in audit history."
} else {
    Write-Fail "Lead edit or audit verification failed."
}

# 7. Workflow Status Transition: Update to Qualified
Write-Host "`n7. Testing Workflow Status Transition (Contacted -> Qualified)..." -ForegroundColor Yellow
$statusToken = Get-AntiForgeryToken "$BaseUrl/Leads/Details/$createdLeadId" $adminSession
$statusBody = @{
    "__RequestVerificationToken" = $statusToken
    "newStatus" = "Qualified"
}

$statusRes = Invoke-WebRequest -Uri "$BaseUrl/Leads/UpdateStatus/$createdLeadId" -Method POST -Body $statusBody -WebSession $adminSession -UseBasicParsing
if ($statusRes.Content -like "*Lead status updated to*Qualified*") {
    Write-Pass "Lead status transitioned to Qualified via workflow action."
} else {
    Write-Fail "Lead status transition to Qualified failed."
}

$detailsAfterStatus = Invoke-WebRequest -Uri "$BaseUrl/Leads/Details/$createdLeadId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterStatus.Content -like "*LeadStatusChanged*") {
    Write-Pass "'LeadStatusChanged' recorded in audit log."
} else {
    Write-Fail "'LeadStatusChanged' missing in audit log."
}

# 8. Lead Conversion Workflow
Write-Host "`n8. Testing Lead Conversion Workflow (Lead -> Customer & Opportunity)..." -ForegroundColor Yellow
$convertUrl = "$BaseUrl/Leads/Convert/$createdLeadId"
$convertGet = Invoke-WebRequest -Uri $convertUrl -WebSession $adminSession -UseBasicParsing
if ($convertGet.Content -like "*Lead Conversion Workflow*") {
    Write-Pass "Convert page loaded with status 200 OK."
} else {
    Write-Fail "Convert page failed to load."
}

$convertToken = Get-AntiForgeryToken $convertUrl $adminSession
$closeDate = (Get-Date).AddDays(45).ToString("yyyy-MM-dd")

$convertBody = @{
    "__RequestVerificationToken" = $convertToken
    "LeadId" = $createdLeadId
    "LeadCode" = $newLeadCode
    "CustomerName" = "Meera Nambiar Menon"
    "Email" = $newLeadEmail
    "Phone" = $newLeadPhone
    "CompanyName" = "CloudCorp Solutions International"
    "Address" = "Tower B, Level 12"
    "City" = "Chennai"
    "State" = "Tamil Nadu"
    "CreateOpportunity" = "true"
    "OpportunityName" = "CloudCorp Multi-Year Enterprise Deal"
    "OpportunityAmount" = "850000.00"
    "OpportunityStage" = "Proposal"
    "Probability" = "60"
    "ExpectedCloseDate" = $closeDate
}

$convertPostRes = Invoke-WebRequest -Uri $convertUrl -Method POST -Body $convertBody -WebSession $adminSession -UseBasicParsing
if ($convertPostRes.Content -like "*Lead converted successfully*" -or $convertPostRes.Content -like "*Customer Details*") {
    Write-Pass "Lead conversion succeeded and redirected with confirmation."
} else {
    Write-Fail "Lead conversion failed to redirect with confirmation."
}

# Verify Converted Lead Status
$leadCheckRes = Invoke-WebRequest -Uri "$BaseUrl/Leads/Details/$createdLeadId" -WebSession $adminSession -UseBasicParsing
if ($leadCheckRes.Content -like "*Converted*" -and $leadCheckRes.Content -like "*LeadConverted*") {
    Write-Pass "Lead status is confirmed 'Converted' and 'LeadConverted' audit log created."
} else {
    Write-Fail "Lead status not updated to Converted or audit log missing."
}

# Verify Created Customer
$custCheckRes = Invoke-WebRequest -Uri "$BaseUrl/Customers?search=$randNum" -WebSession $adminSession -UseBasicParsing
if ($custCheckRes.Content -like "*Meera Nambiar Menon*" -and $custCheckRes.Content -like "*CloudCorp Solutions*") {
    Write-Pass "New Customer record created and visible in Customer Management."
} else {
    Write-Fail "Customer record not found after lead conversion."
}

# 9. Test Re-conversion Prevention
Write-Host "`n9. Testing Prevention of Double-Conversion..." -ForegroundColor Yellow
$reconvertRes = Invoke-WebRequest -Uri "$BaseUrl/Leads/Convert/$createdLeadId" -WebSession $adminSession -UseBasicParsing
if ($reconvertRes.Content -like "*already converted*") {
    Write-Pass "Already-converted lead blocked from second conversion."
} else {
    Write-Fail "System allowed re-conversion of an already converted lead."
}

# 10. SalesExecutive Role Security on Leads
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

$salesLeadsRes = Invoke-WebRequest -Uri "$BaseUrl/Leads" -WebSession $salesSession -UseBasicParsing
if ($salesLeadsRes.StatusCode -eq 200 -and $salesLeadsRes.Content -like "*Lead Management*") {
    Write-Pass "SalesExecutive can access Lead Management."
} else {
    Write-Fail "SalesExecutive unable to access Lead Management."
}

# Attempt unauthorized delete as SalesExecutive
$salesToken2 = Get-AntiForgeryToken "$BaseUrl/Leads" $salesSession
$salesDelRes = Invoke-WebRequest -Uri "$BaseUrl/Leads/Delete/$createdLeadId" -Method POST -Body @{ "__RequestVerificationToken" = $salesToken2 } -WebSession $salesSession -UseBasicParsing
if ($salesDelRes.Content -like "*Only Administrators and Managers are permitted*") {
    Write-Pass "Server-side authorization blocked SalesExecutive delete attempt."
} else {
    Write-Fail "Server-side authorization did not properly reject SalesExecutive delete."
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 5 TESTS PASSED! (0 Failures)" -ForegroundColor Green
} else {
    Write-Host "  PHASE 5 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
}
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($global:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
