$env:DOTNET_ROOT = "C:\Users\kondr\AppData\Local\Microsoft\dotnet"
$env:PATH = "C:\Users\kondr\AppData\Local\Microsoft\dotnet;C:\Users\kondr\.dotnet\tools;" + $env:PATH

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "  Starting AcxiomCRM Application Server   " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Application URL : http://localhost:5175" -ForegroundColor Green
Write-Host "Press Ctrl+C to shut down the server" -ForegroundColor Gray
Write-Host ""

dotnet run
