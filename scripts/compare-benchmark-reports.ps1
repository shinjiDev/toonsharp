# Compare two BenchmarkDotNet GitHub markdown reports (before vs after).
param(
    [string]$Before = "BenchmarkDotNet.Artifacts/results-before-opt/ToonSharp.Benchmarks.ToonSharpBenchmarks-report-github.md",
    [string]$After = "BenchmarkDotNet.Artifacts/results/ToonSharp.Benchmarks.ToonSharpBenchmarks-report-github.md"
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Get-BenchmarkRows {
    param([string]$Path)
    $full = Join-Path $root $Path
    if (-not (Test-Path $full)) {
        throw "Report not found: $full"
    }

    $rows = @{}
    foreach ($line in Get-Content $full) {
        if ($line -match '^\|\s*([\w_]+)\s*\|\s*([\d,\.]+)\s') {
            $name = $Matches[1]
            $mean = [double]($Matches[2] -replace ',', '')
            $rows[$name] = $mean
        }
    }
    return $rows
}

$beforeRows = Get-BenchmarkRows -Path $Before
$afterRows = Get-BenchmarkRows -Path $After

Write-Host "`n=== ToonSharp benchmarks: before vs after ===" -ForegroundColor Cyan
Write-Host ("{0,-28} {1,12} {2,12} {3,10}" -f "Method", "Before (us)", "After (us)", "Delta %")
Write-Host ("-" * 66)

foreach ($name in ($beforeRows.Keys | Sort-Object)) {
    if (-not $afterRows.ContainsKey($name)) { continue }
    $b = $beforeRows[$name]
    $a = $afterRows[$name]
    $pct = if ($b -gt 0) { (($a - $b) / $b) * 100.0 } else { 0 }
    $color = if ($pct -lt -3) { "Green" } elseif ($pct -gt 3) { "Red" } else { "Gray" }
    Write-Host ("{0,-28} {1,12:N1} {2,12:N1} {3,9:N1}%" -f $name, $b, $a, $pct) -ForegroundColor $color
}

Write-Host ""
