using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using ToonSharp;

namespace ToonSharp.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 50, iterationCount: 8)]
public class SpecV3ListItemBenchmarks
{
    private Dictionary<string, object?> _v3ListItemObject = null!;
    private string _v3ListItemToon = null!;

    [GlobalSetup]
    public void Setup()
    {
        _v3ListItemObject = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?>
                {
                    ["users"] = new List<object?>
                    {
                        new Dictionary<string, object?> { ["id"] = 1, ["name"] = "Ada" },
                        new Dictionary<string, object?> { ["id"] = 2, ["name"] = "Bob" },
                        new Dictionary<string, object?> { ["id"] = 3, ["name"] = "Cy" }
                    },
                    ["status"] = "active"
                }
            }
        };

        _v3ListItemToon = Api.ToToon(_v3ListItemObject, indent: 2, mode: "auto");
    }

    [Benchmark]
    public string Serialize_V3ListItemTabular() => Api.ToToon(_v3ListItemObject, indent: 2, mode: "auto");

    [Benchmark]
    public object? Deserialize_V3ListItemTabular() => Api.FromToon(_v3ListItemToon);

    [Benchmark]
    public object? RoundTrip_V3ListItemTabular()
    {
        var toon = Api.ToToon(_v3ListItemObject, indent: 2, mode: "auto");
        return Api.FromToon(toon);
    }
}
