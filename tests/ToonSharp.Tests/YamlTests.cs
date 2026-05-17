using System.Collections.Generic;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class YamlTests
{
    [Fact]
    public void YamlToToon_SimpleObject()
    {
        var yaml = @"
name: Luz
age: 16
active: true
";
        var toon = Api.YamlToToon(yaml);
        
        Assert.Contains("name:", toon);
        Assert.Contains("Luz", toon);
        Assert.Contains("age:", toon);
        Assert.Contains("16", toon);
        Assert.Contains("active:", toon);
        Assert.Contains("true", toon);
    }

    [Fact]
    public void ToonToYaml_SimpleObject()
    {
        var toon = @"
name: Luz
age: 16
active: true
";
        var yaml = Api.ToonToYaml(toon);
        
        Assert.Contains("name:", yaml);
        Assert.Contains("Luz", yaml);
        Assert.Contains("age:", yaml);
        Assert.Contains("16", yaml);
        Assert.Contains("active:", yaml);
        Assert.Contains("true", yaml);
    }

    [Fact]
    public void YamlToToon_WithArray()
    {
        var yaml = @"
crew:
  - id: 1
    name: Luz
  - id: 2
    name: Amity
";
        var toon = Api.YamlToToon(yaml);
        
        Assert.Contains("crew", toon);
        Assert.Contains("Luz", toon);
        Assert.Contains("Amity", toon);
    }

    [Fact]
    public void ToonToYaml_WithArray()
    {
        var toon = @"
crew:
  - id: 1
    name: Luz
  - id: 2
    name: Amity
";
        var yaml = Api.ToonToYaml(toon);
        
        Assert.Contains("crew:", yaml);
        Assert.Contains("Luz", yaml);
        Assert.Contains("Amity", yaml);
    }

    [Fact]
    public void YamlToToon_RoundTrip()
    {
        var originalYaml = @"
name: Luz
age: 16
active: true
items:
  - sword
  - shield
  - potion
";
        // YAML -> TOON -> Object
        var toon = Api.YamlToToon(originalYaml);
        var obj = Api.FromToon(toon);
        
        Assert.NotNull(obj);
        
        // Object -> YAML
        var finalYaml = Api.ToYaml(obj);
        
        Assert.Contains("name:", finalYaml);
        Assert.Contains("Luz", finalYaml);
        Assert.Contains("age:", finalYaml);
        Assert.Contains("16", finalYaml);
        Assert.Contains("items:", finalYaml);
        Assert.Contains("sword", finalYaml);
    }

    [Fact]
    public void ToonToYaml_RoundTrip()
    {
        var originalToon = @"
name: Luz
age: 16
active: true
items:
  - sword
  - shield
  - potion
";
        // TOON -> YAML -> Object
        var yaml = Api.ToonToYaml(originalToon);
        var obj = Api.FromYaml(yaml);
        
        Assert.NotNull(obj);
        
        // Object -> TOON
        var finalToon = Api.ToToon(obj);
        
        Assert.Contains("name:", finalToon);
        Assert.Contains("Luz", finalToon);
        Assert.Contains("age:", finalToon);
        Assert.Contains("16", finalToon);
        Assert.Contains("items[", finalToon);
        Assert.Contains("sword", finalToon);
    }

    [Fact]
    public void FromYaml_ParsesCorrectly()
    {
        var yaml = @"
name: Luz
age: 16
active: true
";
        var obj = Api.FromYaml(yaml);
        
        Assert.NotNull(obj);
    }

    [Fact]
    public void ToYaml_SerializesCorrectly()
    {
        var data = new Dictionary<string, object?>
        {
            ["name"] = "Luz",
            ["age"] = 16,
            ["active"] = true
        };
        
        var yaml = Api.ToYaml(data);
        
        Assert.Contains("name:", yaml);
        Assert.Contains("Luz", yaml);
        Assert.Contains("age:", yaml);
        Assert.Contains("16", yaml);
    }

    [Fact]
    public void YamlToToon_ComplexNestedStructure()
    {
        var yaml = @"
user:
  name: Luz
  profile:
    age: 16
    location: Boiling Isles
  friends:
    - Amity
    - Willow
    - Gus
";
        var toon = Api.YamlToToon(yaml);
        
        Assert.Contains("user:", toon);
        Assert.Contains("name:", toon);
        Assert.Contains("Luz", toon);
        Assert.Contains("profile:", toon);
        Assert.Contains("friends[", toon);
        Assert.Contains("Amity", toon);
    }

    [Fact]
    public void ToonToYaml_ComplexNestedStructure()
    {
        var toon = @"
user:
  name: Luz
  profile:
    age: 16
    location: Boiling Isles
  friends:
    - Amity
    - Willow
    - Gus
";
        var yaml = Api.ToonToYaml(toon);
        
        Assert.Contains("user:", yaml);
        Assert.Contains("name:", yaml);
        Assert.Contains("Luz", yaml);
        Assert.Contains("profile:", yaml);
        Assert.Contains("friends:", yaml);
        Assert.Contains("Amity", yaml);
    }
}

