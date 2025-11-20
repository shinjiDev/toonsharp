# Script to update the performance results in the README
# Run: .\scripts\update-performance-results.ps1

$ErrorActionPreference = 'Stop'

Write-Host "Running benchmarks..." -ForegroundColor Cyan
dotnet run --project benchmarks/ToonSharp.Benchmarks/ToonSharp.Benchmarks.csproj -c Release -- --exporters Markdown --exporters Json | Out-Null

$reportPath = "BenchmarkDotNet.Artifacts\results\ToonSharp.Benchmarks.ToonSharpBenchmarks-report-github.md"
if (-not (Test-Path $reportPath)) {
    Write-Host "Error: Benchmark report was not found" -ForegroundColor Red
    exit 1
}

Write-Host "Benchmarks completed. Results are stored at $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "To update the README manually:" -ForegroundColor Yellow
Write-Host "1. Review the results at: $reportPath" -ForegroundColor Yellow
Write-Host "2. Update the performance table in README.md with the new values" -ForegroundColor Yellow
Write-Host ""
Write-Host "Note: Automatically updating the README requires additional JSON processing." -ForegroundColor Yellow

