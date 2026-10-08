Write-Host "=====================================================================" -ForegroundColor Cyan
Write-Host "         ACXIOMCRM - COMPREHENSIVE SYSTEM TEST SUITE RUNNER           " -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan

$testSuites = @(
    @{ Name = "Phase 2: Authentication & Role Authorization"; Script = "test_phase2.ps1" },
    @{ Name = "Phase 4: Customer Management & Validation"; Script = "test_phase4.ps1" },
    @{ Name = "Phase 5: Lead Lifecycle & Conversion Workflow"; Script = "test_phase5.ps1" },
    @{ Name = "Phase 6: Follow-Up Management & Business Rules"; Script = "test_phase6.ps1" },
    @{ Name = "Phase 7: Opportunity Pipeline & Forecast"; Script = "test_phase7.ps1" },
    @{ Name = "Phase 8: Activity Tracking & Engagement"; Script = "test_phase8.ps1" },
    @{ Name = "Phase 10: Centralized Append-Only Audit Trail"; Script = "test_phase10.ps1" },
    @{ Name = "Phase 11: REST API & Swagger Integration"; Script = "test_phase11.ps1" },
    @{ Name = "Phase 12: Business Reports & CSV Export Engine"; Script = "test_phase12.ps1" },
    @{ Name = "Phase 13: Section 17.19 End-to-End Acceptance Scenarios"; Script = "test_acceptance_scenarios.ps1" }
)

$summary = @()

foreach ($suite in $testSuites) {
    Write-Host "`n>>> Running: $($suite.Name) ..." -ForegroundColor Yellow
    $startTime = Get-Date
    $output = powershell -ExecutionPolicy Bypass -File ".\$($suite.Script)" 2>&1
    $exitCode = $LASTEXITCODE
    $duration = [Math]::Round(((Get-Date) - $startTime).TotalSeconds, 1)

    $status = if ($exitCode -eq 0) { "PASS" } else { "FAIL" }
    
    # Extract passed count if possible
    $match = [regex]::Match($output, 'Passed:\s*(\d+)\s*\/\s*(\d+)')
    $counts = if ($match.Success) { "$($match.Groups[1].Value) / $($match.Groups[2].Value)" } else { "Completed" }

    Write-Host "    Result: $status ($counts) in ${duration}s" -ForegroundColor $(if ($status -eq "PASS") { "Green" } else { "Red" })

    $summary += [PSCustomObject]@{
        Suite = $suite.Name
        Script = $suite.Script
        Status = $status
        Tests = $counts
        DurationSec = $duration
    }
}

Write-Host "`n=====================================================================" -ForegroundColor Cyan
Write-Host "                     MASTER TEST RESULTS REPORT                      " -ForegroundColor Cyan
Write-Host "=====================================================================" -ForegroundColor Cyan

$allPass = $true
foreach ($res in $summary) {
    $color = if ($res.Status -eq "PASS") { "Green" } else { "Red"; $allPass = $false }
    Write-Host ("{0,-50} : [{1}] ({2}, {3}s)" -f $res.Suite, $res.Status, $res.Tests, $res.DurationSec) -ForegroundColor $color
}

Write-Host "=====================================================================" -ForegroundColor Cyan
if ($allPass) {
    Write-Host " ALL TEST SUITES PASSED PERFECTLY! SYSTEM IS 100% HEALTHY." -ForegroundColor Green
} else {
    Write-Host " SOME TESTS FAILED. PLEASE REVIEW LOGS." -ForegroundColor Red
    exit 1
}
