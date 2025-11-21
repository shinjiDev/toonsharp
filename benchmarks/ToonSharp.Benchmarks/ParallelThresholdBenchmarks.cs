using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ToonSharp;

namespace ToonSharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, iterationCount: 10)]
public class ArrayParallelThresholdBenchmarks
{
    private Dictionary<string, object?> _largeArrayObject = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Large array object: 1000+ items to test parallel array serialization
        var arrayItems = new List<object?>();
        for (int i = 1; i <= 1000; i++)
        {
            arrayItems.Add($"Item {i}");
        }
        _largeArrayObject = new Dictionary<string, object?>
        {
            ["items"] = arrayItems
        };
    }

    // Test different thresholds for array serialization
    [Params(50, 100, 150, 200, 250, 300, 400, 500, 750, 1000)]
    public int ArrayThreshold { get; set; }

    [Benchmark]
    [BenchmarkCategory("ArrayThreshold")]
    public string SerializeArray_WithThreshold()
    {
        var serializer = new ToonSerializer(arrayParallelThreshold: ArrayThreshold);
        return serializer.Dumps(_largeArrayObject);
    }
}

[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, iterationCount: 10)]
public class TableParallelThresholdBenchmarks
{
    private Dictionary<string, object?> _largeTableObject = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Large table object: 200+ rows to test parallel table serialization
        var tableRows = new List<Dictionary<string, object?>>();
        for (int i = 1; i <= 200; i++)
        {
            tableRows.Add(new Dictionary<string, object?>
            {
                ["id"] = i,
                ["name"] = $"User {i}",
                ["email"] = $"user{i}@example.com",
                ["active"] = i % 2 == 0,
                ["score"] = i * 1.5
            });
        }
        _largeTableObject = new Dictionary<string, object?>
        {
            ["users"] = tableRows
        };
    }

    // Test different thresholds for table serialization
    [Params(25, 50, 75, 100, 150, 200)]
    public int TableThreshold { get; set; }

    [Benchmark]
    [BenchmarkCategory("TableThreshold")]
    public string SerializeTable_WithThreshold()
    {
        var serializer = new ToonSerializer(tableParallelThreshold: TableThreshold);
        return serializer.Dumps(_largeTableObject);
    }
}
