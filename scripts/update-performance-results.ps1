# Script para actualizar los resultados de rendimiento en el README
# Ejecutar: .\scripts\update-performance-results.ps1

$ErrorActionPreference = 'Stop'

Write-Host "Ejecutando benchmarks..." -ForegroundColor Cyan
dotnet run --project benchmarks/ToonSharp.Benchmarks/ToonSharp.Benchmarks.csproj -c Release -- --exporters Markdown --exporters Json | Out-Null

$reportPath = "BenchmarkDotNet.Artifacts\results\ToonSharp.Benchmarks.ToonSharpBenchmarks-report-github.md"
if (-not (Test-Path $reportPath)) {
    Write-Host "Error: No se encontró el reporte de benchmarks" -ForegroundColor Red
    exit 1
}

Write-Host "Benchmarks completados. Los resultados están en $reportPath" -ForegroundColor Green
Write-Host ""
Write-Host "Para actualizar el README manualmente:" -ForegroundColor Yellow
Write-Host "1. Revisa los resultados en: $reportPath" -ForegroundColor Yellow
Write-Host "2. Actualiza la tabla de rendimiento en README.md con los nuevos valores" -ForegroundColor Yellow
Write-Host ""
Write-Host "Nota: La actualización automática del README requiere procesamiento adicional del JSON." -ForegroundColor Yellow

