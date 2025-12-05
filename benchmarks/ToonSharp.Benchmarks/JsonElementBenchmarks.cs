using System.Collections.Generic;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using ToonSharp;

namespace ToonSharp.Benchmarks;

/// <summary>
/// Benchmarks for JsonElement serialization to TOON.
/// Measures performance of converting JSON strings to TOON format.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, iterationCount: 10)]
public class JsonElementBenchmarks
{
    // JSON strings for testing
    private string _smallJson = null!;
    private string _mediumJson = null!;
    private string _largeJson = null!;
    
    // Pre-deserialized JsonElements
    private object? _smallJsonElement = null!;
    private object? _mediumJsonElement = null!;
    private object? _largeJsonElement = null!;
    
    // Pre-converted dictionaries for comparison
    private Dictionary<string, object?> _smallDict = null!;
    private Dictionary<string, object?> _mediumDict = null!;
    private Dictionary<string, object?> _largeDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small JSON: simple object
        _smallJson = @"{""id"":1,""name"":""Test User"",""active"":true,""score"":98.5}";
        
        // Medium JSON: object with arrays and nested objects
        _mediumJson = JsonSerializer.Serialize(new
        {
            DocumentId = "DOC-2024-001",
            Content = "This is a document about digital transformation in enterprises...",
            AnalysisType = "sentiment_and_topics",
            MaxTokensResponse = 500,
            DesiredMetrics = new[] { "sentiment", "topics", "entities", "summary" },
            Metadata = new
            {
                Author = "John Doe",
                Created = "2024-01-15",
                Tags = new[] { "AI", "ML", "NLP" }
            },
            Settings = new
            {
                Language = "en",
                Model = "gpt-4",
                Temperature = 0.7
            }
        });

        // Large JSON: array of objects
        var users = new List<object>();
        for (int i = 1; i <= 100; i++)
        {
            users.Add(new
            {
                id = i,
                name = $"User {i}",
                email = $"user{i}@example.com",
                role = i % 3 == 0 ? "admin" : "user",
                active = i % 5 != 0,
                score = 50 + (i % 50),
                tags = new[] { $"tag{i % 10}", $"group{i % 5}" }
            });
        }
        _largeJson = JsonSerializer.Serialize(new { users = users });

        // Pre-deserialize for comparison
        _smallJsonElement = JsonSerializer.Deserialize<object>(_smallJson);
        _mediumJsonElement = JsonSerializer.Deserialize<object>(_mediumJson);
        _largeJsonElement = JsonSerializer.Deserialize<object>(_largeJson);

        // Pre-convert to dictionaries for baseline comparison
        _smallDict = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Test User",
            ["active"] = true,
            ["score"] = 98.5
        };

        _mediumDict = new Dictionary<string, object?>
        {
            ["DocumentId"] = "DOC-2024-001",
            ["Content"] = "This is a document about digital transformation in enterprises...",
            ["AnalysisType"] = "sentiment_and_topics",
            ["MaxTokensResponse"] = 500,
            ["DesiredMetrics"] = new List<object?> { "sentiment", "topics", "entities", "summary" },
            ["Metadata"] = new Dictionary<string, object?>
            {
                ["Author"] = "John Doe",
                ["Created"] = "2024-01-15",
                ["Tags"] = new List<object?> { "AI", "ML", "NLP" }
            },
            ["Settings"] = new Dictionary<string, object?>
            {
                ["Language"] = "en",
                ["Model"] = "gpt-4",
                ["Temperature"] = 0.7
            }
        };

        var usersList = new List<object?>();
        for (int i = 1; i <= 100; i++)
        {
            usersList.Add(new Dictionary<string, object?>
            {
                ["id"] = i,
                ["name"] = $"User {i}",
                ["email"] = $"user{i}@example.com",
                ["role"] = i % 3 == 0 ? "admin" : "user",
                ["active"] = i % 5 != 0,
                ["score"] = 50 + (i % 50),
                ["tags"] = new List<object?> { $"tag{i % 10}", $"group{i % 5}" }
            });
        }
        _largeDict = new Dictionary<string, object?> { ["users"] = usersList };
    }

    // ============================================
    // JsonElement Serialization (with normalization)
    // ============================================

    [Benchmark]
    public string JsonElement_ToToon_Small()
    {
        return Api.ToToon(_smallJsonElement);
    }

    [Benchmark]
    public string JsonElement_ToToon_Medium()
    {
        return Api.ToToon(_mediumJsonElement);
    }

    [Benchmark]
    public string JsonElement_ToToon_Large()
    {
        return Api.ToToon(_largeJsonElement);
    }

    // ============================================
    // Full Pipeline: JSON String -> TOON
    // ============================================

    [Benchmark]
    public string JsonToToon_Small()
    {
        return Api.JsonToToon(_smallJson);
    }

    [Benchmark]
    public string JsonToToon_Medium()
    {
        return Api.JsonToToon(_mediumJson);
    }

    [Benchmark]
    public string JsonToToon_Large()
    {
        return Api.JsonToToon(_largeJson);
    }

    // ============================================
    // Baseline: Dictionary Serialization (no normalization)
    // ============================================

    [Benchmark(Baseline = true)]
    public string Dictionary_ToToon_Small()
    {
        return Api.ToToon(_smallDict);
    }

    [Benchmark]
    public string Dictionary_ToToon_Medium()
    {
        return Api.ToToon(_mediumDict);
    }

    [Benchmark]
    public string Dictionary_ToToon_Large()
    {
        return Api.ToToon(_largeDict);
    }
}

