$endpoints = @(
    "http://localhost:5175/Account/Login",
    "http://localhost:5175/Account/Register",
    "http://localhost:5175/swagger/v1/swagger.json"
)

Write-Host "Verifying AcxiomCRM endpoints..." -ForegroundColor Cyan

foreach ($ep in $endpoints) {
    try {
        $res = Invoke-WebRequest -Uri $ep -UseBasicParsing
        Write-Host "  [OK] $ep -> Status $($res.StatusCode)" -ForegroundColor Green
    } catch {
        Write-Host "  [FAIL] $ep -> $($_.Exception.Message)" -ForegroundColor Red
    }
}
