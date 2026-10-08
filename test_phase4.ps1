# test_phase4.ps1
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
Write-Host "  ACXIOMCRM - PHASE 4: CUSTOMER MANAGEMENT TESTS" -ForegroundColor Cyan
Write-Host "=======================================================`n" -ForegroundColor Cyan

# Session for Admin
$adminSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession

# Helper to extract AntiForgeryToken from any page
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
if ($loginRes.Content -like "*Sign Out*" -or $loginRes.Content -like "*Administrator*") {
    Write-Pass "Admin logged in successfully and landed on dashboard."
} else {
    Write-Fail "Admin login failed."
}

# 2. Get Customers Index
Write-Host "`n2. Testing Customer Index (GET /Customers)..." -ForegroundColor Yellow
$indexRes = Invoke-WebRequest -Uri "$BaseUrl/Customers" -WebSession $adminSession -UseBasicParsing
if ($indexRes.StatusCode -eq 200 -and $indexRes.Content -like "*Customer Management*") {
    Write-Pass "Customer Index returned 200 OK with correct title."
} else {
    Write-Fail "Customer Index failed to return expected content."
}

if ($indexRes.Content -like "*CUST-*" -or $indexRes.Content -like "*Rajesh Sharma*" -or $indexRes.Content -like "*Tata Consultancy Services*") {
    Write-Pass "Customer data successfully listed in table."
} else {
    Write-Fail "Customer records not found in list."
}

# 3. Customer Create GET
Write-Host "`n3. Testing Customer Create Form (GET /Customers/Create)..." -ForegroundColor Yellow
$createUrl = "$BaseUrl/Customers/Create"
$createRes = Invoke-WebRequest -Uri $createUrl -WebSession $adminSession -UseBasicParsing
if ($createRes.StatusCode -eq 200 -and $createRes.Content -like "*Create New Customer Account*") {
    Write-Pass "Create form loaded with status 200 OK."
} else {
    Write-Fail "Create form failed to load."
}

$createToken = Get-AntiForgeryToken $createUrl $adminSession
if ($createToken) {
    Write-Pass "Extracted AntiForgeryToken successfully."
} else {
    Write-Fail "Could not extract AntiForgeryToken from create form."
}

# 4. Customer Create POST (Valid Data)
Write-Host "`n4. Testing Customer Creation with Valid Data..." -ForegroundColor Yellow
$randNum = Get-Random -Minimum 5000 -Maximum 9999
$newCustCode = "CUST-$randNum"
$newEmail = "contact-$randNum@starlightaero.com"
$newPhone = "+91 988$randNum"
$createBody = @{
    "__RequestVerificationToken" = $createToken
    "CustomerCode" = $newCustCode
    "CustomerName" = "Starlight Aerospace"
    "Email" = $newEmail
    "Phone" = $newPhone
    "CompanyName" = "Starlight Aerospace Ltd"
    "Address" = "Plot 42, Tech Park"
    "City" = "Bengaluru"
    "State" = "Karnataka"
}

$createPostRes = Invoke-WebRequest -Uri "$BaseUrl/Customers/Create" -Method POST -Body $createBody -WebSession $adminSession -UseBasicParsing
if ($createPostRes.Content -like "*created successfully*" -or $createPostRes.Content -like "*Starlight Aerospace*") {
    Write-Pass "Customer created successfully and redirected to index with success message."
} else {
    Write-Fail "Customer creation failed to redirect with success message."
}

# Verify customer shows up in search
$searchRes = Invoke-WebRequest -Uri "$BaseUrl/Customers?search=$randNum" -WebSession $adminSession -UseBasicParsing
if ($searchRes.Content -like "*Starlight Aerospace*" -and $searchRes.Content -like "*$newEmail*") {
    Write-Pass "Newly created customer found in search results."
} else {
    Write-Fail "Newly created customer not visible in search."
}

# 5. Customer Create POST (Duplicate Email Validation)
Write-Host "`n5. Testing Duplicate Email Validation..." -ForegroundColor Yellow
$dupToken = Get-AntiForgeryToken $createUrl $adminSession
$dupEmailBody = @{
    "__RequestVerificationToken" = $dupToken
    "CustomerCode" = "CUST-" + (Get-Random -Minimum 1000 -Maximum 4999)
    "CustomerName" = "Different Company"
    "Email" = $newEmail # Duplicate!
    "Phone" = "+91 977" + (Get-Random -Minimum 1000 -Maximum 9999)
    "CompanyName" = "Another Co"
}
$dupEmailRes = Invoke-WebRequest -Uri "$BaseUrl/Customers/Create" -Method POST -Body $dupEmailBody -WebSession $adminSession -UseBasicParsing
if ($dupEmailRes.Content -like "*already exists*") {
    Write-Pass "Duplicate email correctly rejected with server-side validation error."
} else {
    Write-Fail "Duplicate email was not rejected as expected."
}

# 6. Customer Create POST (Duplicate Phone Validation)
Write-Host "`n6. Testing Duplicate Phone Validation..." -ForegroundColor Yellow
$dupToken2 = Get-AntiForgeryToken $createUrl $adminSession
$dupPhoneBody = @{
    "__RequestVerificationToken" = $dupToken2
    "CustomerCode" = "CUST-" + (Get-Random -Minimum 1000 -Maximum 4999)
    "CustomerName" = "Phone Dupe Co"
    "Email" = "unique-$randNum@testco.com"
    "Phone" = $newPhone # Duplicate!
    "CompanyName" = "Phone Dupe Ltd"
}
$dupPhoneRes = Invoke-WebRequest -Uri "$BaseUrl/Customers/Create" -Method POST -Body $dupPhoneBody -WebSession $adminSession -UseBasicParsing
if ($dupPhoneRes.Content -like "*already exists*") {
    Write-Pass "Duplicate phone number correctly rejected with server-side validation error."
} else {
    Write-Fail "Duplicate phone was not rejected as expected."
}

# 7. Customer Details & Audit Verification
Write-Host "`n7. Testing Customer Details View..." -ForegroundColor Yellow
# Extract customer ID from search response
$idMatch = [regex]::Match($searchRes.Content, '/Customers/Details/([0-9]+)')
if (-not $idMatch.Success) {
    Write-Fail "Could not find Customer ID in search response."
    $createdId = 1
} else {
    $createdId = $idMatch.Groups[1].Value
    Write-Pass "Found created customer ID: $createdId"
}

$detailsRes = Invoke-WebRequest -Uri "$BaseUrl/Customers/Details/$createdId" -WebSession $adminSession -UseBasicParsing
if ($detailsRes.Content -like "*Starlight Aerospace*" -and $detailsRes.Content -like "*Bengaluru*") {
    Write-Pass "Customer Details page loaded correct customer profile data."
} else {
    Write-Fail "Customer Details page missing expected profile information."
}

if ($detailsRes.Content -like "*Opportunities*" -and $detailsRes.Content -like "*Follow-Ups*" -and $detailsRes.Content -like "*Audit History*") {
    Write-Pass "Customer Details tabs (Opportunities, Follow-Ups, Activities, Audit History) present."
} else {
    Write-Fail "Customer Details tabs missing."
}

if ($detailsRes.Content -like "*CustomerCreated*") {
    Write-Pass "Audit history displays 'CustomerCreated' event."
} else {
    Write-Fail "Audit history missing 'CustomerCreated' event."
}

# 8. Customer Edit
Write-Host "`n8. Testing Customer Edit..." -ForegroundColor Yellow
$editUrl = "$BaseUrl/Customers/Edit/$createdId"
$editToken = Get-AntiForgeryToken $editUrl $adminSession

$editBody = @{
    "__RequestVerificationToken" = $editToken
    "CustomerId" = $createdId
    "CustomerCode" = $newCustCode
    "CustomerName" = "Starlight Aerospace International"
    "Email" = $newEmail
    "Phone" = $newPhone
    "CompanyName" = "Starlight Aerospace International Corp"
    "Address" = "Level 8, Tower A"
    "City" = "Bengaluru"
    "State" = "Karnataka"
    "Status" = "Active"
}

$editPostRes = Invoke-WebRequest -Uri $editUrl -Method POST -Body $editBody -WebSession $adminSession -UseBasicParsing
if ($editPostRes.Content -like "*Starlight Aerospace International*" -and $editPostRes.Content -like "*CustomerUpdated*") {
    Write-Pass "Customer updated successfully and 'CustomerUpdated' recorded in audit log."
} else {
    Write-Fail "Customer update or audit verification failed."
}

# 9. Customer Deactivate
Write-Host "`n9. Testing Customer Deactivate..." -ForegroundColor Yellow
$deactToken = Get-AntiForgeryToken "$BaseUrl/Customers" $adminSession
$deactBody = @{
    "__RequestVerificationToken" = $deactToken
}

$deactRes = Invoke-WebRequest -Uri "$BaseUrl/Customers/Deactivate/$createdId" -Method POST -Body $deactBody -WebSession $adminSession -UseBasicParsing
if ($deactRes.Content -like "*deactivated successfully*") {
    Write-Pass "Customer deactivated flash message displayed."
} else {
    Write-Fail "Customer deactivate failed to show success message."
}

$detailsAfterDeact = Invoke-WebRequest -Uri "$BaseUrl/Customers/Details/$createdId" -WebSession $adminSession -UseBasicParsing
if ($detailsAfterDeact.Content -like "*Inactive*" -and $detailsAfterDeact.Content -like "*CustomerDeactivated*") {
    Write-Pass "Customer status is confirmed Inactive and 'CustomerDeactivated' logged."
} else {
    Write-Fail "Customer deactivate verification failed on details page."
}

# 10. SalesExecutive Role Verification
Write-Host "`n10. Testing SalesExecutive Role Security..." -ForegroundColor Yellow
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

$salesCustRes = Invoke-WebRequest -Uri "$BaseUrl/Customers" -WebSession $salesSession -UseBasicParsing
if ($salesCustRes.StatusCode -eq 200 -and $salesCustRes.Content -like "*Customer Management*") {
    Write-Pass "SalesExecutive can access permitted Customer Management."
} else {
    Write-Fail "SalesExecutive unable to access Customer Management."
}

# SalesExecutive attempts unauthorized hard delete on Customer #1
$salesToken2 = Get-AntiForgeryToken "$BaseUrl/Customers" $salesSession
$salesDelRes = Invoke-WebRequest -Uri "$BaseUrl/Customers/Delete/1" -Method POST -Body @{ "__RequestVerificationToken" = $salesToken2 } -WebSession $salesSession -UseBasicParsing
if ($salesDelRes.Content -like "*Only Administrators and Managers are permitted*") {
    Write-Pass "Server-side authorization blocked SalesExecutive delete attempt with proper message."
} else {
    Write-Fail "Server-side authorization did not properly reject SalesExecutive delete."
}

# Verify Customer 1 is still in database
$cust1Check = Invoke-WebRequest -Uri "$BaseUrl/Customers/Details/1" -WebSession $adminSession -UseBasicParsing
if ($cust1Check.Content -like "*Rajesh Sharma*") {
    Write-Pass "Record intact: Customer was NOT deleted by unauthorized user."
} else {
    Write-Fail "Customer record was improperly deleted!"
}

Write-Host "`n=======================================================" -ForegroundColor Cyan
if ($global:FailedTests -eq 0) {
    Write-Host "  ALL PHASE 4 TESTS PASSED! (0 Failures)" -ForegroundColor Green
} else {
    Write-Host "  PHASE 4 TESTS FAILED WITH $global:FailedTests FAILURES!" -ForegroundColor Red
}
Write-Host "=======================================================`n" -ForegroundColor Cyan

if ($global:FailedTests -gt 0) {
    exit 1
} else {
    exit 0
}
