using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace ToonSharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 1000, iterationCount: 10)]
public class IsInlineBenchmarks
{
    private object?[] _testValues = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create a diverse set of test values
        _testValues = new object?[]
        {
            null,
            "simple string",
            "string with\nnewline",
            "another simple",
            new Dictionary<string, object?>(),
            new List<object?>(),
            new Dictionary<string, object?> { ["key"] = "value" },
            new List<object?> { 1, 2, 3 },
            42,
            3.14,
            true,
            false,
            "very long string without newlines that should be inline",
            "short\nmultiline",
            "",
            "single char",
            new Dictionary<string, object?> { ["a"] = 1, ["b"] = 2 },
            new List<object?> { "a", "b", "c" }
        };
    }

    // Current implementation (using 'is' pattern matching)
    private static bool IsInline_Current(object? value)
    {
        if (value == null)
        {
            return true;
        }

        // Lists and dictionaries are never inline (they need block formatting)
        // Using 'is' pattern matching is faster than GetType() and handles inheritance correctly
        if (value is Dictionary<string, object?> || value is List<object?>)
        {
            return false;
        }

        // IndexOf is slightly faster than Contains for single character checks
        if (value is string str && str.IndexOf('\n') >= 0)
        {
            return false;
        }

        return true;
    }

    // Proposed implementation (using GetType())
    private static bool IsInline_Proposed(object? value)
    {
        if (value == null) return true;
        
        // GetType() is supposedly faster than 'is' for multiple checks
        var type = value.GetType();
        
        // Comparison by type reference (very fast)
        if (type == typeof(Dictionary<string, object?>)) return false;
        if (type == typeof(List<object?>)) return false;
        
        // For strings, using IndexOf is faster than Contains
        // IndexOf is better optimized by the JIT
        if (type == typeof(string))
        {
            return ((string)value).IndexOf('\n') == -1;
        }
        
        return true;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IsInline")]
    public int CurrentImplementation()
    {
        int count = 0;
        foreach (var value in _testValues)
        {
            if (IsInline_Current(value))
                count++;
        }
        return count;
    }

    [Benchmark]
    [BenchmarkCategory("IsInline")]
    public int ProposedImplementation()
    {
        int count = 0;
        foreach (var value in _testValues)
        {
            if (IsInline_Proposed(value))
                count++;
        }
        return count;
    }

    // Test with only null values
    [Benchmark]
    [BenchmarkCategory("IsInline", "NullOnly")]
    public int Current_NullOnly()
    {
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (IsInline_Current(null))
                count++;
        }
        return count;
    }

    [Benchmark]
    [BenchmarkCategory("IsInline", "NullOnly")]
    public int Proposed_NullOnly()
    {
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (IsInline_Proposed(null))
                count++;
        }
        return count;
    }

    // Test with only strings
    [Benchmark]
    [BenchmarkCategory("IsInline", "StringOnly")]
    public int Current_StringOnly()
    {
        int count = 0;
        string[] strings = { "simple", "with\nnewline", "another", "test\nstring" };
        foreach (var str in strings)
        {
            for (int i = 0; i < 250; i++)
            {
                if (IsInline_Current(str))
                    count++;
            }
        }
        return count;
    }

    [Benchmark]
    [BenchmarkCategory("IsInline", "StringOnly")]
    public int Proposed_StringOnly()
    {
        int count = 0;
        string[] strings = { "simple", "with\nnewline", "another", "test\nstring" };
        foreach (var str in strings)
        {
            for (int i = 0; i < 250; i++)
            {
                if (IsInline_Proposed(str))
                    count++;
            }
        }
        return count;
    }

    // Test with only dictionaries and lists
    [Benchmark]
    [BenchmarkCategory("IsInline", "CollectionsOnly")]
    public int Current_CollectionsOnly()
    {
        int count = 0;
        var dict = new Dictionary<string, object?>();
        var list = new List<object?>();
        for (int i = 0; i < 500; i++)
        {
            if (IsInline_Current(dict))
                count++;
            if (IsInline_Current(list))
                count++;
        }
        return count;
    }

    [Benchmark]
    [BenchmarkCategory("IsInline", "CollectionsOnly")]
    public int Proposed_CollectionsOnly()
    {
        int count = 0;
        var dict = new Dictionary<string, object?>();
        var list = new List<object?>();
        for (int i = 0; i < 500; i++)
        {
            if (IsInline_Proposed(dict))
                count++;
            if (IsInline_Proposed(list))
                count++;
        }
        return count;
    }
}

