using BenchmarkDotNet.Running;
using ToonSharp.Benchmarks;

BenchmarkRunner.Run<ToonSharpBenchmarks>();
BenchmarkRunner.Run<IsInlineBenchmarks>();
BenchmarkRunner.Run<ArrayParallelThresholdBenchmarks>();
BenchmarkRunner.Run<TableParallelThresholdBenchmarks>();

