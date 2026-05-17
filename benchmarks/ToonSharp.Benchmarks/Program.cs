using BenchmarkDotNet.Running;
using ToonSharp.Benchmarks;

if (args.Contains("--all"))
{
    var allArgs = args.Where(a => a != "--all").ToArray();
    BenchmarkSwitcher.FromAssembly(typeof(ToonSharpBenchmarks).Assembly).Run(allArgs);
    return;
}

var runArgs = args.Length > 0
    ? args
    : new[] { "--filter", "*ToonSharpBenchmarks*|*SpecV3*" };

BenchmarkRunner.Run(
    new[] { typeof(ToonSharpBenchmarks), typeof(SpecV3ListItemBenchmarks) },
    config: null,
    runArgs);
