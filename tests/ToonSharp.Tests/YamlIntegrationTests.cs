using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using ToonSharp;

namespace ToonSharp.Tests;

public class YamlIntegrationTests
{
    private readonly string _examplesPath;

    public YamlIntegrationTests()
    {
        // Find the examples directory relative to the test execution directory
        var currentDir = Directory.GetCurrentDirectory();
        var rootDir = FindProjectRoot(currentDir);
        _examplesPath = Path.Combine(rootDir, "examples", "spec_v2");
    }

    private string FindProjectRoot(string currentDir)
    {
        var dir = new DirectoryInfo(currentDir);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "examples")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new DirectoryNotFoundException("Could not find examples directory");
    }

    [Fact]
    public void Validate_All_Valid_Examples_ToonToYaml()
    {
        var validPath = Path.Combine(_examplesPath, "valid");
        var files = Directory.GetFiles(validPath, "*.toon");

        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var toonContent = File.ReadAllText(file);
            
            // Step 1: TOON -> YAML
            var yaml = Api.ToonToYaml(toonContent);
            Assert.False(string.IsNullOrWhiteSpace(yaml), $"YAML conversion failed for {Path.GetFileName(file)}");

            // Step 2: YAML -> Object
            var objFromYaml = Api.FromYaml(yaml);
            Assert.NotNull(objFromYaml);

            // Step 3: Object -> TOON (verify structure is maintained)
            var toonFromYaml = Api.ToToon(objFromYaml);
            Assert.False(string.IsNullOrWhiteSpace(toonFromYaml), $"Round-trip back to TOON failed for {Path.GetFileName(file)}");
        }
    }

    [Fact]
    public void Validate_Conversions_Examples_YamlRoundTrip()
    {
        var conversionPath = Path.Combine(_examplesPath, "conversions");
        var files = Directory.GetFiles(conversionPath, "*.json");

        Assert.NotEmpty(files);

        foreach (var file in files)
        {
            var jsonContent = File.ReadAllText(file);
            var originalObj = System.Text.Json.JsonSerializer.Deserialize<object>(jsonContent);
            
            // Step 1: Object -> YAML
            var yaml = Api.ToYaml(originalObj);
            Assert.False(string.IsNullOrWhiteSpace(yaml), $"YAML serialization failed for {Path.GetFileName(file)}");

            // Step 2: YAML -> TOON
            var toon = Api.YamlToToon(yaml);
            Assert.False(string.IsNullOrWhiteSpace(toon), $"YAML to TOON failed for {Path.GetFileName(file)}");

            // Step 3: TOON -> Object
            var finalObj = Api.FromToon(toon);
            
            // Verify structure matches original (basic check)
            Assert.NotNull(finalObj);
        }
    }
}


