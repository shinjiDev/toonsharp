param(
    [string]$ReportPath = "BenchmarkDotNet.Artifacts/results/ToonSharp.Benchmarks.ToonSharpBenchmarks-report-github.md"
)

$MainBaseline = @{
    Deserialize_LargeArray = 435.015
    Serialize_LargeArray   = 372.841
    RoundTrip_LargeArray   = 829.987
    Deserialize_LargeTable = 402.870
    Serialize_LargeTable   = 512.372
    RoundTrip_LargeTable   = 913.173
}

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$full = Join-Path $root $ReportPath
if (-not (Test-Path $full)) { throw "Report not found: $full" }

$branch = @{}
$pattern = '^\|\s*(?<name>[\w_]+)\s*\|\s*(?<val>[\d,\.]+)\s'
foreach ($line in Get-Content $full) {
    $m = [regex]::Match($line, $pattern)
    if ($m.Success) {
        $branch[$m.Groups['name'].Value] = [double]($m.Groups['val'].Value -replace ',', '')
    }
}

Write-Host "`n=== vs main (v1.4.2 baseline) ===" -ForegroundColor Cyan
Write-Host ("{0,-28} {1,12} {2,12} {3,10}" -f "Method", "Main (us)", "Branch (us)", "Delta %")
Write-Host ("-" * 66)

foreach ($name in ($MainBaseline.Keys | Sort-Object)) {
    if (-not $branch.ContainsKey($name)) { continue }
    $mainVal = $MainBaseline[$name]
    $branchVal = $branch[$name]
    $pct = (($branchVal - $mainVal) / $mainVal) * 100.0
    $color = if ($pct -le 20) { "Green" } elseif ($pct -le 30) { "Yellow" } else { "Red" }
    Write-Host ("{0,-28} {1,12:N1} {2,12:N1} {3,9:N1}%" -f $name, $mainVal, $branchVal, $pct) -ForegroundColor $color
}
Write-Host ""