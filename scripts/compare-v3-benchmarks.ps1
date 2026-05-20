# Compare benchmark JSON: baseline (main) vs current (v2.0 / v3)
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

function Get-BenchmarkMeans {
    param([string]$ResultsDir)
    $means = @{}
    Get-ChildItem -Path $ResultsDir -Filter "*-report.json" -Recurse -ErrorAction SilentlyContinue | ForEach-Object {
        $json = Get-Content $_.FullName -Raw | ConvertFrom-Json
        foreach ($bp in $json.Benchmarks) {
            $name = $bp.Descriptor.WorkloadMethod.Name
            $ns = ($bp.Measurements | Where-Object { $_.IterationStage -eq "Result" } | Measure-Object -Property Nanoseconds -Average).Average
            if ($ns) { $means[$name] = [double]$ns / 1000.0 } # microseconds
        }
    }
    return $means
}

$baselineDir = Join-Path $root "BenchmarkDotNet.Artifacts\results-baseline"
$currentDir = Join-Path $root "BenchmarkDotNet.Artifacts\results"

$base = Get-BenchmarkMeans $baselineDir
$curr = Get-BenchmarkMeans $currentDir

$rows = @()
foreach ($key in ($base.Keys + $curr.Keys | Sort-Object -Unique)) {
    if (-not $base.ContainsKey($key) -or -not $curr.ContainsKey($key)) { continue }
    $b = $base[$key]
    $c = $curr[$key]
    $pct = if ($b -gt 0) { [math]::Round((($c - $b) / $b) * 100, 1) } else { 0 }
    $rows += [PSCustomObject]@{ Benchmark = $key; BaselineUs = [math]::Round($b, 3); CurrentUs = [math]::Round($c, 3); DeltaPercent = $pct }
}

$out = Join-Path $root "BenchmarkDotNet.Artifacts\v3-comparison.json"
$rows | ConvertTo-Json -Depth 3 | Set-Content $out -Encoding UTF8
$rows | Format-Table -AutoSize
Write-Host "Written: $out"
