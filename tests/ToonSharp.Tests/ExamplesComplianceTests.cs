using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonSharp;
using Xunit;

namespace ToonSharp.Tests;

public class ExamplesComplianceTests
{
    private static readonly string RepoRoot = LocateRepoRoot();
    private static readonly string ExamplesRoot = Path.Combine(RepoRoot, "examples", "spec_v2");

    public static IEnumerable<object[]> JsonToonPairs => EnumerateJsonToonPairs();
    public static IEnumerable<object[]> ValidExamples => EnumerateToonFiles("valid");
    public static IEnumerable<object[]> InvalidExamples => EnumerateToonFiles("invalid");

    [Theory]
    [MemberData(nameof(JsonToonPairs))]
    public void Official_examples_parse_to_expected_json(string relativeBase)
    {
        var jsonPath = Path.Combine(ExamplesRoot, $"{relativeBase}.json");
        var toonPath = Path.Combine(ExamplesRoot, $"{relativeBase}.toon");

        var expected = NormalizeJson(File.ReadAllText(jsonPath));
        var actual = NormalizeJson(Api.FromToon(File.ReadAllText(toonPath)));

        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"El ejemplo '{relativeBase}' dejó de coincidir con su JSON de referencia.");
    }

    [Theory]
    [MemberData(nameof(JsonToonPairs))]
    public void Reference_json_round_trips_through_serializer(string relativeBase)
    {
        var jsonPath = Path.Combine(ExamplesRoot, $"{relativeBase}.json");
        var expected = NormalizeJson(File.ReadAllText(jsonPath));
        var jsonGraph = LoadJsonGraph(jsonPath);

        var generatedToon = Api.ToToon(jsonGraph, indent: 2, mode: "auto");
        var roundTrip = NormalizeJson(Api.FromToon(generatedToon));

        Assert.True(
            JsonNode.DeepEquals(expected, roundTrip),
            $"El ejemplo '{relativeBase}' ya no produce un TOON equivalente.");
    }

    [Theory]
    [MemberData(nameof(ValidExamples))]
    public void Valid_examples_round_trip_without_loss(string relativePath)
    {
        var toonPath = Path.Combine(ExamplesRoot, relativePath);
        var toonText = File.ReadAllText(toonPath);

        var (isValid, errors) = Api.ValidateToon(toonText);
        Assert.True(isValid, $"El ejemplo válido '{relativePath}' no superó la validación: {string.Join(", ", errors)}");

        var parsed = Api.FromToon(toonText);
        var normalizedOriginal = NormalizeJson(parsed);

        var regenerated = Api.ToToon(parsed, indent: 2, mode: "auto");
        var roundTrip = NormalizeJson(Api.FromToon(regenerated));

        Assert.True(
            JsonNode.DeepEquals(normalizedOriginal, roundTrip),
            $"El ejemplo válido '{relativePath}' perdió información tras el round-trip.");
    }

    [Theory]
    [MemberData(nameof(InvalidExamples))]
    public void Invalid_examples_fail_in_strict_mode(string relativePath)
    {
        var toonPath = Path.Combine(ExamplesRoot, relativePath);
        var toonText = File.ReadAllText(toonPath);

        Assert.Throws<ToonSyntaxError>(() => Api.FromToon(toonText, mode: "strict"));
    }

    private static IEnumerable<object[]> EnumerateJsonToonPairs()
    {
        if (!Directory.Exists(ExamplesRoot))
        {
            yield break;
        }

        foreach (var jsonFile in Directory.EnumerateFiles(ExamplesRoot, "*.json", SearchOption.AllDirectories))
        {
            var toonFile = Path.ChangeExtension(jsonFile, ".toon");
            if (!File.Exists(toonFile))
            {
                continue;
            }

            var relativeBase = Path.GetRelativePath(ExamplesRoot, jsonFile).Replace('\\', '/');
            if (relativeBase.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                relativeBase = relativeBase[..^5];
            }
            yield return new object[] { relativeBase };
        }
    }

    private static IEnumerable<object[]> EnumerateToonFiles(string subdirectory)
    {
        var dir = Path.Combine(ExamplesRoot, subdirectory);
        if (!Directory.Exists(dir))
        {
            yield break;
        }

        foreach (var toonFile in Directory.EnumerateFiles(dir, "*.toon", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(ExamplesRoot, toonFile).Replace('\\', '/');
            yield return new object[] { relative };
        }
    }

    private static object? LoadJsonGraph(string jsonPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
        return ConvertElement(document.RootElement);
    }

    private static object? ConvertElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => ConvertObject(element),
            JsonValueKind.Array => ConvertArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };

    private static Dictionary<string, object?> ConvertObject(JsonElement element) =>
        element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertElement(p.Value));

    private static List<object?> ConvertArray(JsonElement element) =>
        element.EnumerateArray().Select(ConvertElement).ToList();

    private static JsonNode NormalizeJson(string jsonText) =>
        JsonNode.Parse(jsonText) ?? throw new InvalidOperationException("Invalid JSON content.");

    private static JsonNode NormalizeJson(object? graph)
    {
        var json = JsonSerializer.Serialize(graph, new JsonSerializerOptions { WriteIndented = false });
        return JsonNode.Parse(json) ?? throw new InvalidOperationException("Invalid graph serialization result.");
    }

    private static string LocateRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "ToonSharp.sln")))
        {
            dir = Directory.GetParent(dir)?.FullName;
        }

        if (dir == null)
        {
            throw new InvalidOperationException("No se pudo encontrar la raíz del repositorio.");
        }

        return dir;
    }
}

