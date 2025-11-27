using System;
using System.IO;
using System.Text;
using ToonSharp;

namespace ToonSharp.CLI;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var command = args[0].ToLower();

        try
        {
            return command switch
            {
                "to" => HandleToCommand(args),
                "from" => HandleFromCommand(args),
                "fmt" => HandleFmtCommand(args),
                "yaml-to-toon" => HandleYamlToToonCommand(args),
                "toon-to-yaml" => HandleToonToYamlCommand(args),
                "toml-to-toon" => HandleTomlToToonCommand(args),
                "toon-to-toml" => HandleToonToTomlCommand(args),
                _ => PrintUsage()
            };
        }
        catch (ToonSyntaxError ex)
        {
            Console.Error.WriteLine($"TOON syntax error: {ex.Message}");
            return 2;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 3;
        }
    }

    static int HandleToCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string mode = "auto";
        int indent = 2;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--indent" when i + 1 < args.Length:
                    indent = int.Parse(args[++i]);
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var jsonText = File.ReadAllText(inputFile);
        var obj = System.Text.Json.JsonSerializer.Deserialize<object>(jsonText);
        var toon = Api.ToToon(obj, indent, mode);
        File.WriteAllText(outputFile, toon, Encoding.UTF8);

        return 0;
    }

    static int HandleFromCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string mode = "strict";

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--permissive":
                    mode = "permissive";
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var toonText = File.ReadAllText(inputFile);
        var obj = Api.FromToon(toonText, mode);
        var json = System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outputFile, json, Encoding.UTF8);

        return 0;
    }

    static int HandleFmtCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string mode = "readable";
        int indent = 2;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--indent" when i + 1 < args.Length:
                    indent = int.Parse(args[++i]);
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var toonText = File.ReadAllText(inputFile);
        var obj = Api.FromToon(toonText);
        var formatted = Api.ToToon(obj, indent, mode);
        File.WriteAllText(outputFile, formatted, Encoding.UTF8);

        return 0;
    }

    static int HandleYamlToToonCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string mode = "auto";
        int indent = 2;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--indent" when i + 1 < args.Length:
                    indent = int.Parse(args[++i]);
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var yamlText = File.ReadAllText(inputFile);
        var toon = Api.YamlToToon(yamlText, indent, mode);
        File.WriteAllText(outputFile, toon, Encoding.UTF8);

        return 0;
    }

    static int HandleToonToYamlCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string mode = "strict";

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--permissive":
                    mode = "permissive";
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var toonText = File.ReadAllText(inputFile);
        var yaml = Api.ToonToYaml(toonText, mode);
        File.WriteAllText(outputFile, yaml, Encoding.UTF8);

        return 0;
    }

    static int HandleTomlToToonCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;
        string mode = "auto";
        int indent = 2;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
                case "--mode" when i + 1 < args.Length:
                    mode = args[++i];
                    break;
                case "--indent" when i + 1 < args.Length:
                    indent = int.Parse(args[++i]);
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var tomlText = File.ReadAllText(inputFile);
        var toon = Api.TomlToToon(tomlText, indent, mode);
        File.WriteAllText(outputFile, toon, Encoding.UTF8);

        return 0;
    }

    static int HandleToonToTomlCommand(string[] args)
    {
        string? inputFile = null;
        string? outputFile = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--in" when i + 1 < args.Length:
                    inputFile = args[++i];
                    break;
                case "--out" when i + 1 < args.Length:
                    outputFile = args[++i];
                    break;
            }
        }

        if (inputFile == null || outputFile == null)
        {
            Console.Error.WriteLine("Error: --in and --out are required");
            return 4;
        }

        var toonText = File.ReadAllText(inputFile);
        var toml = Api.ToonToToml(toonText);
        File.WriteAllText(outputFile, toml, Encoding.UTF8);

        return 0;
    }

    static int PrintUsage()
    {
        Console.WriteLine("ToonSharp CLI - JSON ↔ TOON ↔ YAML ↔ TOML conversion tool");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  toonsharp to --in <input.json> --out <output.toon> [--mode auto|compact|readable] [--indent <n>]");
        Console.WriteLine("  toonsharp from --in <input.toon> --out <output.json> [--permissive]");
        Console.WriteLine("  toonsharp fmt --in <input.toon> --out <output.toon> [--mode auto|compact|readable] [--indent <n>]");
        Console.WriteLine("  toonsharp yaml-to-toon --in <input.yaml> --out <output.toon> [--mode auto|compact|readable] [--indent <n>]");
        Console.WriteLine("  toonsharp toon-to-yaml --in <input.toon> --out <output.yaml> [--permissive]");
        Console.WriteLine("  toonsharp toml-to-toon --in <input.toml> --out <output.toon> [--mode auto|compact|readable] [--indent <n>]");
        Console.WriteLine("  toonsharp toon-to-toml --in <input.toon> --out <output.toml>");
        return 1;
    }
}
