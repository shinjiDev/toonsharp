namespace ToonSharp;

public sealed class ToonEncodeOptions
{
    public int Indent { get; init; } = 2;
    public string Mode { get; init; } = "auto";
    public string? Delimiter { get; init; }
    public string KeyFolding { get; init; } = "off";
    public int FlattenDepth { get; init; } = int.MaxValue;

    public static ToonEncodeOptions Default { get; } = new();
}

public sealed class ToonDecodeOptions
{
    public int Indent { get; init; } = 2;
    public bool Strict { get; init; } = true;
    public string ExpandPaths { get; init; } = "safe";

    public string ParserMode => Strict ? "strict" : "permissive";

    public bool ExpandPathsSafe => string.Equals(ExpandPaths, "safe", StringComparison.OrdinalIgnoreCase);

    public static ToonDecodeOptions Default { get; } = new();
}