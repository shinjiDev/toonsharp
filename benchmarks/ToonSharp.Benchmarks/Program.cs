using BenchmarkDotNet.Running;
using ToonSharp.Benchmarks;

BenchmarkRunner.Run<ToonSharpBenchmarks>();
BenchmarkRunner.Run<YamlBenchmarks>();
BenchmarkRunner.Run<TomlBenchmarks>();
BenchmarkRunner.Run<IsInlineBenchmarks>();
BenchmarkRunner.Run<ArrayParallelThresholdBenchmarks>();
BenchmarkRunner.Run<TableParallelThresholdBenchmarks>();

