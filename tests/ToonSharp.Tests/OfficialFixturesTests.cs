using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using ToonSharp;
using Xunit;

namespace ToonSharp.Tests;

public class OfficialFixturesTests
{
    private static readonly string FixturesRoot = LocateFixturesRoot();

    public static IEnumerable<object[]> EncodeCases => LoadCases("encode");
    public static IEnumerable<object[]> DecodeCases => LoadCases("decode");

    [Theory]
    [MemberData(nameof(EncodeCases))]
    public void Official_encode_fixture(string file, string testName, JsonElement testCase)
    {
        var shouldError = testCase.TryGetProperty("shouldError", out var errProp) && errProp.GetBoolean();
        var input = ConvertJsonElement(testCase.GetProperty("input"));
        var options = ParseEncodeOptions(testCase);

        if (shouldError)
        {
            Assert.ThrowsAny<Exception>(() => Api.ToToon(input, options));
            return;
        }

        var expected = NormalizeToon(testCase.GetProperty("expected").GetString() ?? string.Empty);
        var actual = NormalizeToon(Api.ToToon(input, options));
        Assert.True(
            expected == actual,
            $"Encode fixture '{file}' / '{testName}' mismatch.\n--- expected ---\n{expected}\n--- actual ---\n{actual}");
    }

    [Theory]
    [MemberData(nameof(DecodeCases))]
    public void Official_decode_fixture(string file, string testName, JsonElement testCase)
    {
        var shouldError = testCase.TryGetProperty("shouldError", out var errProp) && errProp.GetBoolean();
        var input = testCase.GetProperty("input").GetString() ?? string.Empty;
        var options = ParseDecodeOptions(testCase);

        if (shouldError)
        {
            Assert.Throws<ToonSyntaxError>(() => Api.FromToon(input, options));
            return;
        }

        var expected = ToJsonNode(testCase.GetProperty("expected"));
        var actual = ToJsonNode(Api.FromToon(input, options));
        Assert.True(
            JsonNode.DeepEquals(expected, actual),
            $"Decode fixture '{file}' / '{testName}' mismatch.\nexpected: {expected}\nactual:   {actual}");
    }

    private static IEnumerable<object[]> LoadCases(string category)
    {
        var dir = Path.Combine(FixturesRoot, category);
        if (!Directory.Exists(dir))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(dir, "*.json").OrderBy(Path.GetFileName))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            var relativeFile = Path.GetRelativePath(FixturesRoot, file).Replace('\\', '/');
            foreach (var test in doc.RootElement.GetProperty("tests").EnumerateArray())
            {
                var name = test.GetProperty("name").GetString() ?? "unnamed";
                yield return new object[] { relativeFile, name, test.Clone() };
            }
        }
    }

    private static ToonEncodeOptions ParseEncodeOptions(JsonElement testCase)
    {
        if (!testCase.TryGetProperty("options", out var opt))
        {
            return ToonEncodeOptions.Default;
        }

        int indent = opt.TryGetProperty("indent", out var indentProp) ? indentProp.GetInt32() : 2;
        string? delimiter = null;
        if (opt.TryGetProperty("delimiter", out var delimProp))
        {
            delimiter = delimProp.GetString() switch
            {
                "\t" => "\t",
                "|" => "|",
                _ => ",",
            };
        }

        string keyFolding = opt.TryGetProperty("keyFolding", out var foldProp)
            ? foldProp.GetString() ?? "off"
            : "off";

        int flattenDepth = int.MaxValue;
        if (opt.TryGetProperty("flattenDepth", out var depthProp) && depthProp.ValueKind == JsonValueKind.Number)
        {
            flattenDepth = depthProp.GetInt32();
        }

        return new ToonEncodeOptions
        {
            Indent = indent,
            Mode = "auto",
            Delimiter = delimiter,
            KeyFolding = keyFolding,
            FlattenDepth = flattenDepth,
        };
    }

    private static ToonDecodeOptions ParseDecodeOptions(JsonElement testCase)
    {
        if (!testCase.TryGetProperty("options", out var opt))
        {
            return ToonDecodeOptions.Default;
        }

        bool strict = !opt.TryGetProperty("strict", out var strictProp) || strictProp.GetBoolean();
        string expandPaths = opt.TryGetProperty("expandPaths", out var expandProp)
            ? expandProp.GetString() ?? "safe"
            : "safe";

        return new ToonDecodeOptions
        {
            Strict = strict,
            ExpandPaths = expandPaths,
        };
    }

    private static string NormalizeToon(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).TrimEnd('\n');

    private static JsonNode ToJsonNode(object? graph)
    {
        var json = JsonSerializer.Serialize(graph);
        return JsonNode.Parse(json) ?? throw new InvalidOperationException("Invalid graph.");
    }

    private static object? ConvertJsonElement(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElement(p.Value)),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElement).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null,
        };

    private static string LocateFixturesRoot()
    {
        var outputFixtures = Path.Combine(AppContext.BaseDirectory, "fixtures", "spec");
        if (Directory.Exists(outputFixtures))
        {
            return outputFixtures;
        }

        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "tests", "fixtures", "spec");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Official spec fixtures not found under tests/fixtures/spec.");
    }
}
