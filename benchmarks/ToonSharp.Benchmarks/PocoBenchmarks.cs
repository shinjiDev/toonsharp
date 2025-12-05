using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ToonSharp;

namespace ToonSharp.Benchmarks;

/// <summary>
/// Benchmarks for POCO (Plain Old CLR Object) serialization to TOON.
/// Measures performance of converting .NET classes to TOON format via reflection.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(invocationCount: 100, iterationCount: 10)]
public class PocoBenchmarks
{
    // Test POCO classes
    public class SimplePerson
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool Active { get; set; }
        public double Score { get; set; }
    }

    public class ComplexDocument
    {
        public string DocumentId { get; set; } = "";
        public string Content { get; set; } = "";
        public string AnalysisType { get; set; } = "";
        public int MaxTokensResponse { get; set; }
        public List<string> DesiredMetrics { get; set; } = new();
        public DocumentMetadata Metadata { get; set; } = new();
        public DocumentSettings Settings { get; set; } = new();
    }

    public class DocumentMetadata
    {
        public string Author { get; set; } = "";
        public string Created { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }

    public class DocumentSettings
    {
        public string Language { get; set; } = "";
        public string Model { get; set; } = "";
        public double Temperature { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public bool Active { get; set; }
        public int Score { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class UsersContainer
    {
        public List<User> Users { get; set; } = new();
    }

    // Test objects
    private SimplePerson _smallPoco = null!;
    private ComplexDocument _mediumPoco = null!;
    private UsersContainer _largePoco = null!;

    // Equivalent dictionaries for comparison
    private Dictionary<string, object?> _smallDict = null!;
    private Dictionary<string, object?> _mediumDict = null!;
    private Dictionary<string, object?> _largeDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small POCO
        _smallPoco = new SimplePerson
        {
            Id = 1,
            Name = "Test User",
            Active = true,
            Score = 98.5
        };

        // Medium POCO
        _mediumPoco = new ComplexDocument
        {
            DocumentId = "DOC-2024-001",
            Content = "This is a document about digital transformation in enterprises...",
            AnalysisType = "sentiment_and_topics",
            MaxTokensResponse = 500,
            DesiredMetrics = new List<string> { "sentiment", "topics", "entities", "summary" },
            Metadata = new DocumentMetadata
            {
                Author = "John Doe",
                Created = "2024-01-15",
                Tags = new List<string> { "AI", "ML", "NLP" }
            },
            Settings = new DocumentSettings
            {
                Language = "en",
                Model = "gpt-4",
                Temperature = 0.7
            }
        };

        // Large POCO
        _largePoco = new UsersContainer { Users = new List<User>() };
        for (int i = 1; i <= 100; i++)
        {
            _largePoco.Users.Add(new User
            {
                Id = i,
                Name = $"User {i}",
                Email = $"user{i}@example.com",
                Role = i % 3 == 0 ? "admin" : "user",
                Active = i % 5 != 0,
                Score = 50 + (i % 50),
                Tags = new List<string> { $"tag{i % 10}", $"group{i % 5}" }
            });
        }

        // Equivalent dictionaries for baseline comparison
        _smallDict = new Dictionary<string, object?>
        {
            ["Id"] = 1,
            ["Name"] = "Test User",
            ["Active"] = true,
            ["Score"] = 98.5
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
                ["Id"] = i,
                ["Name"] = $"User {i}",
                ["Email"] = $"user{i}@example.com",
                ["Role"] = i % 3 == 0 ? "admin" : "user",
                ["Active"] = i % 5 != 0,
                ["Score"] = 50 + (i % 50),
                ["Tags"] = new List<object?> { $"tag{i % 10}", $"group{i % 5}" }
            });
        }
        _largeDict = new Dictionary<string, object?> { ["Users"] = usersList };
    }

    // ============================================
    // POCO Serialization (with reflection-based normalization)
    // ============================================

    [Benchmark]
    public string Poco_ToToon_Small()
    {
        return Api.ToToon(_smallPoco);
    }

    [Benchmark]
    public string Poco_ToToon_Medium()
    {
        return Api.ToToon(_mediumPoco);
    }

    [Benchmark]
    public string Poco_ToToon_Large()
    {
        return Api.ToToon(_largePoco);
    }

    // ============================================
    // Baseline: Dictionary Serialization (no reflection)
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

    // ============================================
    // Anonymous Types (also uses reflection)
    // ============================================

    [Benchmark]
    public string Anonymous_ToToon_Small()
    {
        var anon = new { Id = 1, Name = "Test User", Active = true, Score = 98.5 };
        return Api.ToToon(anon);
    }

    [Benchmark]
    public string Anonymous_ToToon_Medium()
    {
        var anon = new
        {
            DocumentId = "DOC-2024-001",
            Content = "This is a document about digital transformation in enterprises...",
            AnalysisType = "sentiment_and_topics",
            MaxTokensResponse = 500,
            DesiredMetrics = new[] { "sentiment", "topics", "entities", "summary" }
        };
        return Api.ToToon(anon);
    }
}

