using System.Collections.Generic;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class ApiTests
{
    [Fact]
    public void ToToon_SimpleObject()
    {
        var data = new Dictionary<string, object?>
        {
            ["name"] = "Luz",
            ["age"] = 16
        };

        var toon = Api.ToToon(data);
        Assert.Contains("name:", toon);
        Assert.Contains("Luz", toon);
    }

    [Fact]
    public void FromToon_SimpleObject()
    {
        var toon = @"name: Luz
age: 16";
        var result = Api.FromToon(toon) as Dictionary<string, object?>;
        
        Assert.NotNull(result);
        Assert.Equal("Luz", result["name"]);
        Assert.Equal(16L, result["age"]);
    }

    [Fact]
    public void ValidateToon_Valid()
    {
        var toon = @"name: Luz
age: 16";
        var (isValid, errors) = Api.ValidateToon(toon);
        
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateToon_Invalid()
    {
        var toon = "invalid syntax without colon";
        var (isValid, errors) = Api.ValidateToon(toon);
        
        Assert.False(isValid);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void SuggestTabular_UniformArray()
    {
        var data = new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = 1, ["name"] = "Luz" },
            new Dictionary<string, object?> { ["id"] = 2, ["name"] = "Amity" }
        };

        var suggestion = Api.SuggestTabular(data);
        Assert.True(suggestion.UseTabular);
        Assert.Equal(2, suggestion.Keys.Count);
    }

    [Fact]
    public void SuggestTabular_NonUniformArray()
    {
        var data = new List<object?>
        {
            new Dictionary<string, object?> { ["id"] = 1 },
            new Dictionary<string, object?> { ["id"] = 2, ["name"] = "Amity" }
        };

        var suggestion = Api.SuggestTabular(data);
        Assert.False(suggestion.UseTabular);
    }
}

