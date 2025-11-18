using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using ToonSharp;

namespace ToonSharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, iterationCount: 10)]
public class ToonSharpBenchmarks
{
    private Dictionary<string, object?> _smallObject = null!;
    private Dictionary<string, object?> _mediumObject = null!;
    private Dictionary<string, object?> _largeObject = null!;
    private string _smallToon = null!;
    private string _mediumToon = null!;
    private string _largeToon = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small object: ~100 bytes
        _smallObject = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Test User",
            ["active"] = true,
            ["score"] = 98.5
        };

        // Medium object: ~1KB
        _mediumObject = new Dictionary<string, object?>
        {
            ["users"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Alice", ["role"] = "admin", ["active"] = true },
                new() { ["id"] = 2, ["name"] = "Bob", ["role"] = "user", ["active"] = true },
                new() { ["id"] = 3, ["name"] = "Charlie", ["role"] = "user", ["active"] = false },
                new() { ["id"] = 4, ["name"] = "Diana", ["role"] = "admin", ["active"] = true },
                new() { ["id"] = 5, ["name"] = "Eve", ["role"] = "user", ["active"] = true }
            },
            ["metadata"] = new Dictionary<string, object?>
            {
                ["version"] = "1.0.0",
                ["timestamp"] = 1234567890L,
                ["count"] = 5
            }
        };

        // Large object: ~10KB (using nested structures to avoid tabular format issues)
        var largeItems = new List<Dictionary<string, object?>>();
        for (int i = 1; i <= 50; i++)
        {
            // Vary structure slightly to prevent tabular format
            var item = new Dictionary<string, object?>
            {
                ["id"] = i,
                ["name"] = $"Item {i}",
                ["value"] = i * 1.5
            };
            if (i % 3 == 0)
            {
                item["extra"] = $"extra-{i}";
            }
            largeItems.Add(item);
        }
        _largeObject = new Dictionary<string, object?>
        {
            ["items"] = largeItems,
            ["metadata"] = new Dictionary<string, object?>
            {
                ["version"] = "2.0.0",
                ["timestamp"] = 1234567890L,
                ["total"] = 50,
                ["settings"] = new Dictionary<string, object?>
                {
                    ["theme"] = "dark",
                    ["notifications"] = true,
                    ["language"] = "en"
                }
            }
        };

        // Pre-generate TOON strings for parsing benchmarks
        _smallToon = Api.ToToon(_smallObject);
        _mediumToon = Api.ToToon(_mediumObject);
        _largeToon = Api.ToToon(_largeObject);
    }

    // Serialization benchmarks (JSON -> TOON)
    [Benchmark]
    [BenchmarkCategory("Serialization", "Small")]
    public string Serialize_Small() => Api.ToToon(_smallObject);

    [Benchmark]
    [BenchmarkCategory("Serialization", "Medium")]
    public string Serialize_Medium() => Api.ToToon(_mediumObject);

    [Benchmark]
    [BenchmarkCategory("Serialization", "Large")]
    public string Serialize_Large() => Api.ToToon(_largeObject);

    // Deserialization benchmarks (TOON -> JSON)
    [Benchmark]
    [BenchmarkCategory("Deserialization", "Small")]
    public object? Deserialize_Small() => Api.FromToon(_smallToon);

    [Benchmark]
    [BenchmarkCategory("Deserialization", "Medium")]
    public object? Deserialize_Medium() => Api.FromToon(_mediumToon);

    [Benchmark]
    [BenchmarkCategory("Deserialization", "Large")]
    public object? Deserialize_Large() => Api.FromToon(_largeToon);

    // Round-trip benchmarks
    [Benchmark]
    [BenchmarkCategory("RoundTrip", "Small")]
    public object? RoundTrip_Small()
    {
        var toon = Api.ToToon(_smallObject);
        return Api.FromToon(toon);
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip", "Medium")]
    public object? RoundTrip_Medium()
    {
        var toon = Api.ToToon(_mediumObject);
        return Api.FromToon(toon);
    }

    [Benchmark]
    [BenchmarkCategory("RoundTrip", "Large")]
    public object? RoundTrip_Large()
    {
        var toon = Api.ToToon(_largeObject);
        return Api.FromToon(toon);
    }
}

