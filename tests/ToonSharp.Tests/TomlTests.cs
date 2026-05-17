using System;
using System.Collections.Generic;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class TomlTests
{
    [Fact]
    public void TomlToToon_SimpleObject()
    {
        var toml = @"
name = ""Luz""
age = 16
active = true
";
        var toon = Api.TomlToToon(toml, indent: 2, mode: "readable");
        
        Assert.Contains("name: Luz", toon);
        Assert.Contains("age: 16", toon);
        Assert.Contains("active: true", toon);
    }

    [Fact]
    public void ToonToToml_SimpleObject()
    {
        var toon = @"
name: Luz
age: 16
active: true
";
        var toml = Api.ToonToToml(toon);
        
        Assert.Contains("name = \"Luz\"", toml);
        Assert.Contains("age = 16", toml);
        Assert.Contains("active = true", toml);
    }

    [Fact]
    public void TomlToToon_Array()
    {
        var toml = @"
colors = [""red"", ""green"", ""blue""]
";
        var toon = Api.TomlToToon(toml, indent: 2, mode: "readable");
        
        Assert.Contains("colors[3]: red,green,blue", toon);
        Assert.Contains("red", toon);
        Assert.Contains("green", toon);
        Assert.Contains("blue", toon);
    }

    [Fact]
    public void TomlToToon_NestedTable()
    {
        var toml = @"
[database]
server = ""localhost""
port = 5432

[database.credentials]
username = ""admin""
password = ""secret""
";
        var toon = Api.TomlToToon(toml, indent: 2, mode: "readable");
        
        Assert.Contains("database:", toon);
        Assert.Contains("server: localhost", toon);
        Assert.Contains("port: 5432", toon);
        Assert.Contains("credentials:", toon);
        Assert.Contains("username: admin", toon);
        Assert.Contains("password: secret", toon);
    }

    [Fact]
    public void ToonToToml_NestedObject()
    {
        var toon = @"
database:
  server: localhost
  port: 5432
  credentials:
    username: admin
    password: secret
";
        var toml = Api.ToonToToml(toon);
        
        Assert.Contains("server = \"localhost\"", toml);
        Assert.Contains("port = 5432", toml);
        Assert.Contains("username = \"admin\"", toml);
        Assert.Contains("password = \"secret\"", toml);
    }

    [Fact]
    public void TomlToToon_RoundTrip()
    {
        var originalToml = @"
name = ""ToonSharp""
version = ""1.3.0""
active = true
";
        var toon = Api.TomlToToon(originalToml, indent: 2, mode: "readable");
        var toml = Api.ToonToToml(toon);
        var toonAgain = Api.TomlToToon(toml, indent: 2, mode: "readable");
        
        Assert.Contains("name:", toonAgain);
        Assert.Contains("ToonSharp", toonAgain);
        Assert.Contains("version:", toonAgain);
        Assert.Contains("1.3.0", toonAgain);
        Assert.Contains("active: true", toonAgain);
    }

    [Fact]
    public void ToToml_FromDictionary()
    {
        var data = new Dictionary<string, object?>
        {
            ["name"] = "Luz",
            ["age"] = 16,
            ["active"] = true
        };

        var toml = Api.ToToml(data);
        
        Assert.Contains("name = \"Luz\"", toml);
        Assert.Contains("age = 16", toml);
        Assert.Contains("active = true", toml);
    }

    [Fact]
    public void FromToml_ToDictionary()
    {
        var toml = @"
name = ""Luz""
age = 16
active = true
";
        var obj = Api.FromToml(toml);
        
        Assert.NotNull(obj);
        var dict = obj as Dictionary<string, object?>;
        Assert.NotNull(dict);
        Assert.Equal("Luz", dict!["name"]);
        Assert.Equal(16L, dict["age"]);
        Assert.Equal(true, dict["active"]);
    }

    [Fact]
    public void TomlToToon_ArrayOfTables()
    {
        var toml = @"
[[products]]
name = ""Hammer""
price = 9.99

[[products]]
name = ""Nail""
price = 0.99
";
        var toon = Api.TomlToToon(toml, indent: 2, mode: "readable");
        
        Assert.Contains("products:", toon);
        Assert.Contains("Hammer", toon);
        Assert.Contains("9.99", toon);
        Assert.Contains("Nail", toon);
        Assert.Contains("0.99", toon);
    }

    [Fact]
    public void TomlToToon_ComplexStructure()
    {
        var toml = @"
title = ""TOML Example""

[owner]
name = ""Tom Preston-Werner""
dob = 1979-05-27T07:32:00-08:00

[database]
server = ""192.168.1.1""
ports = [ 8001, 8001, 8002 ]
connection_max = 5000
enabled = true

[servers]

  [servers.alpha]
  ip = ""10.0.0.1""
  dc = ""eqdc10""

  [servers.beta]
  ip = ""10.0.0.2""
  dc = ""eqdc10""
";
        var toon = Api.TomlToToon(toml, indent: 2, mode: "readable");
        
        Assert.Contains("title:", toon);
        Assert.Contains("TOML Example", toon);
        Assert.Contains("owner:", toon);
        Assert.Contains("Tom Preston-Werner", toon);
        Assert.Contains("database:", toon);
        Assert.Contains("192.168.1.1", toon);
        Assert.Contains("servers:", toon);
        Assert.Contains("alpha:", toon);
        Assert.Contains("beta:", toon);
    }
}

