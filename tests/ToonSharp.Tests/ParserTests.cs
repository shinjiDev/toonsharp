using System.Collections.Generic;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class ParserTests
{
    [Fact]
    public void ParseSimpleObject()
    {
        var text = @"
name: Luz
age: 16
active: true
";
        var parser = new ToonParser();
        var result = parser.Parse(text) as Dictionary<string, object?>;
        
        Assert.NotNull(result);
        Assert.Equal("Luz", result["name"]);
        Assert.Equal(16L, result["age"]);
        Assert.Equal(true, result["active"]);
    }

    [Fact]
    public void ParseTableBlock()
    {
        // Test without leading newline first
        var text1 = @"crew[2]{id,name}:
  1,Luz
  2,Amity
";
        var parser1 = new ToonParser();
        var result1 = parser1.Parse(text1) as Dictionary<string, object?>;
        
        Assert.NotNull(result1);
        Assert.True(result1.ContainsKey("crew"), "Result should contain 'crew' key");
        var crew1 = result1["crew"] as List<Dictionary<string, object?>>;
        Assert.NotNull(crew1);
        Assert.Equal(2, crew1.Count);
        
        var first1 = crew1[0] as Dictionary<string, object?>;
        Assert.NotNull(first1);
        Assert.Equal(1L, first1["id"]);
        Assert.Equal("Luz", first1["name"]);
        
        // Test with leading newline (should work the same)
        var text2 = @"
crew[2]{id,name}:
  1,Luz
  2,Amity
";
        var parser2 = new ToonParser();
        var result2 = parser2.Parse(text2) as Dictionary<string, object?>;
        
        Assert.NotNull(result2);
        Assert.True(result2.ContainsKey("crew"), "Result should contain 'crew' key");
        var crew2 = result2["crew"] as List<Dictionary<string, object?>>;
        Assert.NotNull(crew2);
        Assert.Equal(2, crew2.Count);
        
        var first2 = crew2[0] as Dictionary<string, object?>;
        Assert.NotNull(first2);
        Assert.Equal(1L, first2["id"]);
        Assert.Equal("Luz", first2["name"]);
    }

    [Fact]
    public void ParseNestedObject()
    {
        var text = @"
ship:
  name: ""Owl House""
  location: Bonesborough
";
        var parser = new ToonParser();
        var result = parser.Parse(text) as Dictionary<string, object?>;
        
        Assert.NotNull(result);
        var ship = result["ship"] as Dictionary<string, object?>;
        Assert.NotNull(ship);
        Assert.Equal("Owl House", ship["name"]);
        Assert.Equal("Bonesborough", ship["location"]);
    }

    [Fact]
    public void ParseArray()
    {
        var text = @"
items:
  - name: light
    power: 5
  - name: fire
    power: 9
";
        var parser = new ToonParser();
        var result = parser.Parse(text) as Dictionary<string, object?>;
        
        Assert.NotNull(result);
        var items = result["items"] as List<object?>;
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public void ParseThrowsOnInvalidSyntax()
    {
        var text = "a:\n  user";
        var parser = new ToonParser();
        
        Assert.Throws<ToonSyntaxError>(() => parser.Parse(text));
    }
}

