# Runs core TOON benchmarks and writes Markdown + JSON under BenchmarkDotNet.Artifacts
param(
    [string]$Filter = "*ToonSharpBenchmarks*",
    [string]$ArtifactsSubDir = "."
)

$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")

Push-Location $root
try {
    Stop-Process -Name "ToonSharp.Benchmarks" -Force -ErrorAction SilentlyContinue
    dotnet build benchmarks/ToonSharp.Benchmarks/ToonSharp.Benchmarks.csproj -c Release -v q | Out-Null

    $log = Join-Path $root "BenchmarkDotNet.Artifacts\benchmark-run.log"
    New-Item -ItemType Directory -Force -Path (Split-Path $log) | Out-Null

    $args = @(
        "run", "-c", "Release",
        "--project", "benchmarks/ToonSharp.Benchmarks/ToonSharp.Benchmarks.csproj",
        "--no-build",
        "--",
        "--filter", $Filter,
        "--job", "short",
        "--warmupCount", "1",
        "--iterationCount", "5",
        "--launchCount", "1",
        "--unrollFactor", "1",
        "--exporters", "json",
        "--exporters", "markdown"
    )

    & dotnet @args 2>&1 | Tee-Object -FilePath $log
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Write-Host "Done. Reports under BenchmarkDotNet.Artifacts/results/" -ForegroundColor Green
}
finally {
    Pop-Location
}
