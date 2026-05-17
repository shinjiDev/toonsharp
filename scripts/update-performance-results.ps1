# Script to update the performance results in the README
# Run: .\scripts\update-performance-results.ps1

$ErrorActionPreference = 'Stop'

Write-Host "Running core TOON + v3 benchmarks..." -ForegroundColor Cyan
dotnet run --project benchmarks/ToonSharp.Benchmarks/ToonSharp.Benchmarks.csproj -c Release -- --job short --warmupCount 1 --iterationCount 5 --exporters Markdown --exporters Json | Out-Null

$reportPath = "BenchmarkDotNet.Artifacts\results\ToonSharp.Benchmarks.ToonSharpBenchmarks-report-github.md"
$v3Path = "BenchmarkDotNet.Artifacts\results\ToonSharp.Benchmarks.SpecV3ListItemBenchmarks-report-github.md"
if (-not (Test-Path $reportPath)) {
    Write-Host "Error: Benchmark report was not found" -ForegroundColor Red
    exit 1
}

Write-Host "Benchmarks completed." -ForegroundColor Green
Write-Host "  Core: $reportPath" -ForegroundColor Green
if (Test-Path $v3Path) { Write-Host "  v3 §10: $v3Path" -ForegroundColor Green }
Write-Host ""
Write-Host "To update the README manually:" -ForegroundColor Yellow
Write-Host "1. Review the results at: $reportPath" -ForegroundColor Yellow
Write-Host "2. Update the performance table in README.md with the new values" -ForegroundColor Yellow
Write-Host ""
Write-Host "Note: Automatically updating the README requires additional JSON processing." -ForegroundColor Yellow

