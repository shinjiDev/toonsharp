using System;
using System.Collections.Generic;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class SerializerTests
{
    [Fact]
    public void SerializeSimpleObject()
    {
        var data = new Dictionary<string, object?>
        {
            ["name"] = "Luz",
            ["age"] = 16,
            ["active"] = true
        };

        var serializer = new ToonSerializer();
        var result = serializer.Dumps(data);

        Assert.Contains("name:", result);
        Assert.Contains("Luz", result);
        Assert.Contains("age:", result);
        Assert.Contains("active:", result);
    }

    [Fact]
    public void SerializeTableArray()
    {
        var data = new Dictionary<string, object?>
        {
            ["crew"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Luz" },
                new() { ["id"] = 2, ["name"] = "Amity" }
            }
        };

        var serializer = new ToonSerializer();
        var result = serializer.Dumps(data);

        Assert.Contains("crew[2]", result);
        Assert.Contains("id,name", result);
    }

    [Fact]
    public void RoundTrip()
    {
        var original = new Dictionary<string, object?>
        {
            ["name"] = "Luz",
            ["age"] = 16,
            ["active"] = true
        };

        var serializer = new ToonSerializer();
        var toon = serializer.Dumps(original);

        var parser = new ToonParser();
        var parsed = parser.Parse(toon) as Dictionary<string, object?>;

        Assert.NotNull(parsed);
        Assert.Equal("Luz", parsed["name"]);
        Assert.Equal(16L, parsed["age"]);
        Assert.Equal(true, parsed["active"]);
    }
}

