using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ToonSharp;

namespace ToonSharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, iterationCount: 10)]
public class YamlBenchmarks
{
    private Dictionary<string, object?> _smallObject = null!;
    private Dictionary<string, object?> _mediumObject = null!;
    private Dictionary<string, object?> _largeObject = null!;
    private string _smallYaml = null!;
    private string _mediumYaml = null!;
    private string _largeYaml = null!;
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

        // Large object: ~10KB
        var largeItems = new List<Dictionary<string, object?>>();
        for (int i = 1; i <= 50; i++)
        {
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

        // Pre-generate YAML and TOON strings
        _smallYaml = Api.ToYaml(_smallObject);
        _mediumYaml = Api.ToYaml(_mediumObject);
        _largeYaml = Api.ToYaml(_largeObject);
        _smallToon = Api.ToToon(_smallObject);
        _mediumToon = Api.ToToon(_mediumObject);
        _largeToon = Api.ToToon(_largeObject);
    }

    // YAML -> TOON conversion benchmarks
    [Benchmark]
    [BenchmarkCategory("YamlToToon", "Small")]
    public string YamlToToon_Small() => Api.YamlToToon(_smallYaml);

    [Benchmark]
    [BenchmarkCategory("YamlToToon", "Medium")]
    public string YamlToToon_Medium() => Api.YamlToToon(_mediumYaml);

    [Benchmark]
    [BenchmarkCategory("YamlToToon", "Large")]
    public string YamlToToon_Large() => Api.YamlToToon(_largeYaml);

    // TOON -> YAML conversion benchmarks
    [Benchmark]
    [BenchmarkCategory("ToonToYaml", "Small")]
    public string ToonToYaml_Small() => Api.ToonToYaml(_smallToon);

    [Benchmark]
    [BenchmarkCategory("ToonToYaml", "Medium")]
    public string ToonToYaml_Medium() => Api.ToonToYaml(_mediumToon);

    [Benchmark]
    [BenchmarkCategory("ToonToYaml", "Large")]
    public string ToonToYaml_Large() => Api.ToonToYaml(_largeToon);

    // YAML serialization benchmarks
    [Benchmark]
    [BenchmarkCategory("YamlSerialization", "Small")]
    public string ToYaml_Small() => Api.ToYaml(_smallObject);

    [Benchmark]
    [BenchmarkCategory("YamlSerialization", "Medium")]
    public string ToYaml_Medium() => Api.ToYaml(_mediumObject);

    [Benchmark]
    [BenchmarkCategory("YamlSerialization", "Large")]
    public string ToYaml_Large() => Api.ToYaml(_largeObject);

    // YAML deserialization benchmarks
    [Benchmark]
    [BenchmarkCategory("YamlDeserialization", "Small")]
    public object? FromYaml_Small() => Api.FromYaml(_smallYaml);

    [Benchmark]
    [BenchmarkCategory("YamlDeserialization", "Medium")]
    public object? FromYaml_Medium() => Api.FromYaml(_mediumYaml);

    [Benchmark]
    [BenchmarkCategory("YamlDeserialization", "Large")]
    public object? FromYaml_Large() => Api.FromYaml(_largeYaml);

    // Round-trip benchmarks (YAML -> TOON -> YAML)
    [Benchmark]
    [BenchmarkCategory("YamlRoundTrip", "Small")]
    public string YamlRoundTrip_Small()
    {
        var toon = Api.YamlToToon(_smallYaml);
        return Api.ToonToYaml(toon);
    }

    [Benchmark]
    [BenchmarkCategory("YamlRoundTrip", "Medium")]
    public string YamlRoundTrip_Medium()
    {
        var toon = Api.YamlToToon(_mediumYaml);
        return Api.ToonToYaml(toon);
    }

    [Benchmark]
    [BenchmarkCategory("YamlRoundTrip", "Large")]
    public string YamlRoundTrip_Large()
    {
        var toon = Api.YamlToToon(_largeYaml);
        return Api.ToonToYaml(toon);
    }
}

