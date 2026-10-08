$ErrorActionPreference = "Stop"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "    ACXIOMCRM - PHASE 11 REST APIS & SWAGGER TESTS   " -ForegroundColor Cyan
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
    # 1. Test Swagger Endpoint
    Write-Host "`n--- Testing Swagger OpenApi Documentation ---" -ForegroundColor Yellow
    $swaggerResp = Invoke-WebRequest -Uri "$baseUrl/swagger/v1/swagger.json" -Method Get -UseBasicParsing
    Test-Assert "Swagger OpenAPI JSON accessible" ($swaggerResp.StatusCode -eq 200) "Status: $($swaggerResp.StatusCode)"
    
    $swaggerJson = $swaggerResp.Content | ConvertFrom-Json
    $hasCustomerEndpoints = $swaggerJson.paths.PSObject.Properties.Name -contains "/api/customers"
    $hasLeadEndpoints = $swaggerJson.paths.PSObject.Properties.Name -contains "/api/leads"
    $hasOppEndpoints = $swaggerJson.paths.PSObject.Properties.Name -contains "/api/opportunities"
    Test-Assert "Swagger registers CRM endpoints" ($hasCustomerEndpoints -and $hasLeadEndpoints -and $hasOppEndpoints) "Endpoints registered in Swagger spec"

    # 2. Test Unauthorized API Access
    Write-Host "`n--- Testing Unauthorized API Access Protection ---" -ForegroundColor Yellow
    $unauthProtected = $false
    try {
        $unauthResp = Invoke-WebRequest -Uri "$baseUrl/api/customers" -Method Get -MaximumRedirection 0 -UseBasicParsing
        $unauthProtected = ($unauthResp.StatusCode -eq 401 -or $unauthResp.StatusCode -eq 302)
    } catch {
        $msg = "$_"
        if ($msg -like "*redirection*" -or $msg -like "*401*" -or $msg -like "*302*" -or $msg -like "*Unauthorized*") {
            $unauthProtected = $true
        }
    }
    Test-Assert "Unauthenticated API request rejected/redirected" $unauthProtected "Protected from anonymous access"

    # 3. Authenticate as Admin
    Write-Host "`n--- Authenticating as Admin ---" -ForegroundColor Yellow
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $loginPage = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -WebSession $session -UseBasicParsing
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
    $loginResp = Invoke-WebRequest -Uri "$baseUrl/Account/Login" -Method Post -Body $loginBody -WebSession $session -UseBasicParsing
    Test-Assert "Admin login successful" ($loginResp.StatusCode -eq 200 -and $session.Cookies.Count -gt 0) "Session cookies: $($session.Cookies.Count)"

    # 4. Customers API: GET and POST
    Write-Host "`n--- Testing Customers API ---" -ForegroundColor Yellow
    $custGetResp = Invoke-WebRequest -Uri "$baseUrl/api/customers" -Method Get -WebSession $session -UseBasicParsing
    $custGetJson = $custGetResp.Content | ConvertFrom-Json
    Test-Assert "GET /api/customers returns success envelope" ($custGetJson.success -eq $true) "Total items in payload: $($custGetJson.data.Count)"

    $uniqueSuffix = [DateTime]::UtcNow.Ticks.ToString().Substring(10)
    $newCustomerPayload = @{
        customerName = "API Corp $uniqueSuffix"
        email = "api_client_$uniqueSuffix@example.com"
        phone = "+1-555-$uniqueSuffix"
        companyName = "API Technologies Inc"
        address = "100 Cloud Street"
        city = "San Jose"
        state = "CA"
    } | ConvertTo-Json

    $custPostResp = Invoke-WebRequest -Uri "$baseUrl/api/customers" -Method Post -Body $newCustomerPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
    $custPostJson = $custPostResp.Content | ConvertFrom-Json
    $createdCustId = $custPostJson.data.customerId
    Test-Assert "POST /api/customers creates customer (HTTP 201)" ($custPostResp.StatusCode -eq 201 -and $createdCustId -gt 0) "CustomerId: $createdCustId, Code: $($custPostJson.data.customerCode)"

    # Test Duplicate Validation via API
    $dupRejected = $false
    try {
        $dupResp = Invoke-WebRequest -Uri "$baseUrl/api/customers" -Method Post -Body $newCustomerPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
        $dupRejected = ($dupResp.StatusCode -eq 400)
    } catch {
        $msg = "$_"
        if ($msg -like "*400*" -or $msg -like "*Bad Request*") {
            $dupRejected = $true
        }
    }
    Test-Assert "POST /api/customers duplicate email rejected (HTTP 400)" $dupRejected "Duplicate prevented"

    # 5. Leads API: GET, POST and Conversion
    Write-Host "`n--- Testing Leads API & Lead Conversion ---" -ForegroundColor Yellow
    $newLeadPayload = @{
        leadName = "Lead For API $uniqueSuffix"
        email = "lead_api_$uniqueSuffix@growth.org"
        phone = "+1-777-$uniqueSuffix"
        companyName = "Growth Ventures"
        source = "Webinar"
        priority = "High"
        expectedValue = 75000.00
    } | ConvertTo-Json

    $leadPostResp = Invoke-WebRequest -Uri "$baseUrl/api/leads" -Method Post -Body $newLeadPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
    $leadPostJson = $leadPostResp.Content | ConvertFrom-Json
    $createdLeadId = $leadPostJson.data.leadId
    Test-Assert "POST /api/leads creates lead (HTTP 201)" ($leadPostResp.StatusCode -eq 201 -and $createdLeadId -gt 0) "LeadId: $createdLeadId, Code: $($leadPostJson.data.leadCode)"

    # Update lead status to Qualified before conversion
    $qualifyLeadPayload = @{
        leadName = "Lead For API $uniqueSuffix"
        email = "lead_api_$uniqueSuffix@growth.org"
        phone = "+1-777-$uniqueSuffix"
        companyName = "Growth Ventures"
        source = "Webinar"
        priority = "High"
        expectedValue = 75000.00
        status = "Qualified"
    } | ConvertTo-Json
    $leadPutResp = Invoke-WebRequest -Uri "$baseUrl/api/leads/$createdLeadId" -Method Put -Body $qualifyLeadPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
    Test-Assert "PUT /api/leads qualifies lead" ($leadPutResp.StatusCode -eq 200) "Status: $($leadPutResp.StatusCode)"

    # Convert Lead via API
    $convertPayload = @{
        createOpportunity = $true
        opportunityName = "Growth Enterprise Deal $uniqueSuffix"
        opportunityAmount = 75000.00
        expectedCloseDate = (Get-Date).AddMonths(1).ToString("yyyy-MM-ddTHH:mm:ss")
    } | ConvertTo-Json
    $convertResp = Invoke-WebRequest -Uri "$baseUrl/api/leads/$createdLeadId/convert" -Method Post -Body $convertPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
    $convertJson = $convertResp.Content | ConvertFrom-Json
    Test-Assert "POST /api/leads/{id}/convert converts lead" ($convertResp.StatusCode -eq 200 -and $convertJson.data.customerId -gt 0 -and $convertJson.data.opportunityId -gt 0) "CustId: $($convertJson.data.customerId), OppId: $($convertJson.data.opportunityId)"

    # 6. Opportunity API: GET, POST, Validation, and Stage PATCH
    Write-Host "`n--- Testing Opportunities API ---" -ForegroundColor Yellow
    # Invalid amount validation test (Amount <= 0)
    $invalidOppPayload = @{
        opportunityName = "Invalid Zero Deal"
        customerId = $createdCustId
        amount = 0.00
        stage = "Qualification"
        probability = 20
        expectedCloseDate = (Get-Date).AddDays(15).ToString("yyyy-MM-ddTHH:mm:ss")
    } | ConvertTo-Json
    $invalidOppRejected = $false
    try {
        $invalidOppResp = Invoke-WebRequest -Uri "$baseUrl/api/opportunities" -Method Post -Body $invalidOppPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
        $invalidOppRejected = ($invalidOppResp.StatusCode -eq 400)
    } catch {
        $msg = "$_"
        if ($msg -like "*400*" -or $msg -like "*Bad Request*") {
            $invalidOppRejected = $true
        }
    }
    Test-Assert "POST /api/opportunities Amount <= 0 rejected (HTTP 400)" $invalidOppRejected "Amount > 0 business rule enforced"

    # Valid Opportunity Creation
    $newOppPayload = @{
        opportunityName = "API Cloud Migration $uniqueSuffix"
        customerId = $createdCustId
        amount = 54000.00
        stage = "Proposal"
        probability = 50
        expectedCloseDate = (Get-Date).AddDays(20).ToString("yyyy-MM-ddTHH:mm:ss")
    } | ConvertTo-Json
    $oppPostResp = Invoke-WebRequest -Uri "$baseUrl/api/opportunities" -Method Post -Body $newOppPayload -ContentType "application/json" -WebSession $session -UseBasicParsing
    $oppPostJson = $oppPostResp.Content | ConvertFrom-Json
    $createdOppId = $oppPostJson.data.opportunityId
    Test-Assert "POST /api/opportunities creates opportunity (HTTP 201)" ($oppPostResp.StatusCode -eq 201 -and $createdOppId -gt 0) "OppId: $createdOppId, Weighted: $($oppPostJson.data.weightedPipeline)"

    # PATCH Stage to Won
    $patchStagePayload = @{ stage = "Won" } | ConvertTo-Json
    $patchResp = Invoke-WebRequest -Uri "$baseUrl/api/opportunities/$createdOppId/stage" -Method Patch -Body $patchStagePayload -ContentType "application/json" -WebSession $session -UseBasicParsing
    Test-Assert "PATCH /api/opportunities/{id}/stage updates stage to Won" ($patchResp.StatusCode -eq 200) "Status: $($patchResp.StatusCode)"

    $verifyOppResp = Invoke-WebRequest -Uri "$baseUrl/api/opportunities/$createdOppId" -Method Get -WebSession $session -UseBasicParsing
    $verifyOppJson = $verifyOppResp.Content | ConvertFrom-Json
    Test-Assert "Stage Won auto-syncs probability to 100% and status to Won" ($verifyOppJson.data.stage -eq "Won" -and $verifyOppJson.data.probability -eq 100 -and $verifyOppJson.data.status -eq "Won") "Stage: $($verifyOppJson.data.stage), Prob: $($verifyOppJson.data.probability), Status: $($verifyOppJson.data.status)"

    # 7. Authorization Check: SalesExecutive Delete Restriction
    Write-Host "`n--- Testing Role Enforcement on APIs ---" -ForegroundColor Yellow
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
    
    # Sales Executive tries to DELETE opportunity -> Should be 403 or redirect to AccessDenied
    $delForbidden = $false
    try {
        $delResp = Invoke-WebRequest -Uri "$baseUrl/api/opportunities/$createdOppId" -Method Delete -WebSession $salesSession -MaximumRedirection 0 -UseBasicParsing
        $delForbidden = ($delResp.StatusCode -eq 403 -or $delResp.StatusCode -eq 302)
    } catch {
        $msg = "$_"
        if ($msg -like "*403*" -or $msg -like "*Forbidden*" -or $msg -like "*redirection*" -or $msg -like "*AccessDenied*") {
            $delForbidden = $true
        }
    }
    Test-Assert "SalesExecutive DELETE /api/opportunities/{id} denied (Forbidden/302 AccessDenied)" $delForbidden "Role enforcement verified"

} catch {
    Write-Host "FATAL ERROR during test execution: $_" -ForegroundColor Red
    $script:results += [PSCustomObject]@{ Test = "Execution"; Status = "FAIL"; Details = $_.ToString() }
}

Write-Host "`n====================================================" -ForegroundColor Cyan
Write-Host "                PHASE 11 TEST SUMMARY               " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan
$passed = ($results | Where-Object { $_.Status -eq "PASS" }).Count
$total = $results.Count
Write-Host "Passed: $passed / $total" -ForegroundColor $(if ($passed -eq $total) { "Green" } else { "Red" })

if ($passed -ne $total) {
    exit 1
}
