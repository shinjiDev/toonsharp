using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using ToonSharp;

namespace ToonSharp.Benchmarks;

[MemoryDiagnoser]
public class TomlBenchmarks
{
    private Dictionary<string, object?> _smallData = null!;
    private Dictionary<string, object?> _mediumData = null!;
    private Dictionary<string, object?> _largeData = null!;
    
    private string _smallToml = null!;
    private string _mediumToml = null!;
    private string _largeToml = null!;
    
    private string _smallToon = null!;
    private string _mediumToon = null!;
    private string _largeToon = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small data (~100 bytes)
        _smallData = new Dictionary<string, object?>
        {
            ["name"] = "Luz",
            ["age"] = 16,
            ["active"] = true,
            ["score"] = 95.5
        };

        // Medium data (~1KB)
        _mediumData = new Dictionary<string, object?>
        {
            ["title"] = "Configuration File",
            ["version"] = "1.0.0",
            ["database"] = new Dictionary<string, object?>
            {
                ["host"] = "localhost",
                ["port"] = 5432,
                ["username"] = "admin",
                ["password"] = "secret123",
                ["pool_size"] = 10,
                ["timeout"] = 30
            },
            ["server"] = new Dictionary<string, object?>
            {
                ["host"] = "0.0.0.0",
                ["port"] = 8080,
                ["workers"] = 4,
                ["max_connections"] = 1000
            },
            ["logging"] = new Dictionary<string, object?>
            {
                ["level"] = "info",
                ["file"] = "/var/log/app.log",
                ["max_size"] = 10485760,
                ["rotate"] = true
            }
        };

        // Large data (~10KB) - Cargo.toml-like structure
        var dependencies = new Dictionary<string, object?>();
        for (int i = 0; i < 50; i++)
        {
            dependencies[$"package{i}"] = new Dictionary<string, object?>
            {
                ["version"] = $"{i % 3 + 1}.{i % 10}.{i % 5}",
                ["features"] = new List<object?> { "default", "full", "derive" }
            };
        }

        _largeData = new Dictionary<string, object?>
        {
            ["package"] = new Dictionary<string, object?>
            {
                ["name"] = "large-project",
                ["version"] = "0.1.0",
                ["edition"] = "2021",
                ["authors"] = new List<object?> { "Developer 1", "Developer 2", "Developer 3" }
            },
            ["dependencies"] = dependencies,
            ["dev-dependencies"] = new Dictionary<string, object?>
            {
                ["criterion"] = "0.5",
                ["proptest"] = "1.0",
                ["mockall"] = "0.11"
            },
            ["build-dependencies"] = new Dictionary<string, object?>
            {
                ["cc"] = "1.0",
                ["pkg-config"] = "0.3"
            }
        };

        // Pre-serialize for deserialization benchmarks
        _smallToml = Api.ToToml(_smallData);
        _mediumToml = Api.ToToml(_mediumData);
        _largeToml = Api.ToToml(_largeData);

        _smallToon = Api.ToToon(_smallData, indent: 2, mode: "auto");
        _mediumToon = Api.ToToon(_mediumData, indent: 2, mode: "auto");
        _largeToon = Api.ToToon(_largeData, indent: 2, mode: "auto");
    }

    // ============================================
    // TOON → TOML Conversion
    // ============================================

    [Benchmark]
    public string ToonToToml_Small() => Api.ToonToToml(_smallToon);

    [Benchmark]
    public string ToonToToml_Medium() => Api.ToonToToml(_mediumToon);

    [Benchmark]
    public string ToonToToml_Large() => Api.ToonToToml(_largeToon);

    // ============================================
    // TOML → TOON Conversion
    // ============================================

    [Benchmark]
    public string TomlToToon_Small() => Api.TomlToToon(_smallToml, indent: 2, mode: "auto");

    [Benchmark]
    public string TomlToToon_Medium() => Api.TomlToToon(_mediumToml, indent: 2, mode: "auto");

    [Benchmark]
    public string TomlToToon_Large() => Api.TomlToToon(_largeToml, indent: 2, mode: "auto");

    // ============================================
    // TOML Serialization (Direct)
    // ============================================

    [Benchmark]
    public string ToToml_Small() => Api.ToToml(_smallData);

    [Benchmark]
    public string ToToml_Medium() => Api.ToToml(_mediumData);

    [Benchmark]
    public string ToToml_Large() => Api.ToToml(_largeData);

    // ============================================
    // TOML Deserialization (Direct)
    // ============================================

    [Benchmark]
    public object? FromToml_Small() => Api.FromToml(_smallToml);

    [Benchmark]
    public object? FromToml_Medium() => Api.FromToml(_mediumToml);

    [Benchmark]
    public object? FromToml_Large() => Api.FromToml(_largeToml);

    // ============================================
    // Round-Trip Conversion
    // ============================================

    [Benchmark]
    public string TomlRoundTrip_Small()
    {
        var toml = Api.ToToml(_smallData);
        var obj = Api.FromToml(toml);
        return Api.ToToml(obj);
    }

    [Benchmark]
    public string TomlRoundTrip_Medium()
    {
        var toml = Api.ToToml(_mediumData);
        var obj = Api.FromToml(toml);
        return Api.ToToml(obj);
    }

    [Benchmark]
    public string TomlRoundTrip_Large()
    {
        var toml = Api.ToToml(_largeData);
        var obj = Api.FromToml(toml);
        return Api.ToToml(obj);
    }
}

