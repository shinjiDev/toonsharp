# 🔄 ToonSharp (C#)

[![NuGet Version](https://img.shields.io/nuget/v/ToonLib.svg?label=NuGet&color=blue)](https://www.nuget.org/packages/ToonLib)
[![Version](https://img.shields.io/badge/ToonLib-2.0.0-blue.svg)](https://www.nuget.org/packages/ToonLib)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ToonLib.svg?label=Downloads&color=green)](https://www.nuget.org/packages/ToonLib)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-528%20passing-brightgreen.svg)](https://github.com/shinjiDev/toonsharp/actions)

**ToonLib 2.0.0** is a major release of the production-grade C# library and CLI for JSON, YAML, TOML, and TOON (Token-Oriented Object Notation). It implements **[TOON SPEC v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md)** with breaking encode/decode changes versus **1.4.x**.

> **Versioning:** **2.0.0** = NuGet package / library. **SPEC v3.0** = format rules this release targets. The published **SPEC v2** example corpus is documented in **[README.v2.md](README.v2.md)** (not the same as library 1.4.x).

**✅ TOON SPEC v3.0** — Canonical §10 list-item encoding, array length headers (`key[N]:`), **358** official encode/decode fixtures, span-optimized I/O, and **528** automated tests.

## ✅ TOON SPEC v3.0 compliance

Conformance is enforced by automated tests, not documentation alone:

| Suite | Coverage |
|-------|----------|
| **`OfficialFixturesTests`** | **358** cases from `tests/fixtures/spec/` (official encode + decode JSON corpora) |
| **`ExamplesComplianceTests`** | **66** cases from `examples/spec_v2/` (published spec examples) |
| **`SpecV3ListItemTests`** | §10 list-item encode/decode and round-trip |
| **Unit tests** | Parser, serializer, API, POCO, YAML/TOML integration |
| **Total** | **528** tests (`dotnet test -c Release`) |

### Encode / decode behavior aligned with the spec

- **§10 list items:** tabular first field on the hyphen line (`- users[2]{id,name}:`), rows at depth +2, siblings at depth +1.
- **Array headers:** `key[N]:` (and delimiter suffix `key[N|]:`, `key[N\t]:` when configured).
- **§7.2 strings:** safe unquoted strings (including spaces); quote when ambiguous (`true`, `42`, leading zeros), when containing structural characters (`:`, `,`, newlines), or inside active delimiter fields.
- **§11 delimiters:** prefer comma; **quote tabular cells** that contain the active delimiter instead of silently switching delimiters; explicit `delimiter` option for `|`, tab, or comma.
- **§7.3 keys:** quote unsafe keys (spaces, leading `-`, `build-system`, etc.); dot-separated foldable keys per key-folding rules.
- **Key folding (`safe`):** collision-aware flattening; `expandPaths` off by default for fixtures, `safe` for example-driven `FromToon`.
- **Numbers:** JSON-style integer width (`long`); leading-zero tokens remain strings.
- **Parser (strict):** missing `:` in object context; multiple root values; blank-line rules in arrays/tables; `- [N]:` nested array headers before inline bracket lines; JSON-quoted root scalars (e.g. Windows paths).
- **Performance (decode):** fast path for **unquoted comma-separated inline arrays** (e.g. `items[1000]: Item 1,Item 2,…`) without per-character state machine cost.
- **Performance (encode):** `ToonWriter` buffer, `AppendScalar` into `StringBuilder`, inline primitive arrays without length caps.

## ✨ Features

The `ToonSharp` library provides comprehensive JSON ↔ TOON ↔ YAML ↔ TOML conversion capabilities:

### 🔧 1. Lossless Conversion

* **Bidirectional conversion** between JSON, YAML, TOML, and TOON formats
* **Round-trip preservation** - data integrity guaranteed
* Supports all JSON/YAML/TOML data types (objects, arrays, scalars)
* Handles nested structures of any depth
* **YAML support** - Convert YAML ↔ TOON seamlessly
* **TOML support** - Convert TOML ↔ TOON for configuration files (Rust Cargo.toml, Python pyproject.toml)

### 📊 2. Advanced Parser & Lexer

* **Recursive descent parser** with indentation tracking
* **Comment support** - inline (`#`, `//`) and block (`/* */`) comments
* **ABNF-backed grammar** - fully compliant with TOON SPEC v3.0
* **Error reporting** with line and column numbers

### 🚀 3. Automatic Tabular Detection

* **Smart detection** of uniform-object arrays
* **Automatic emission** of efficient tabular mode (`key[N]{fields}:`)
* **Token savings estimation**
* **Configurable modes**: auto, compact, readable

### ⚡ 4. Performance Optimizations

* **Span-based lexer and scalar parsing** — `ReadOnlySpan<char>` hot paths, pre-sized line lists
* **Unquoted inline-array fast path** — comma-split decode for `key[N]: a,b,c` without quotes (~**66% faster** large-array deserialize vs ToonLib 1.4.2; see benchmarks)
* **`ToonWriter` + `AppendScalar`** — serialize directly into a shared buffer (large inline arrays **~43% faster** serialize vs ToonLib 1.4.2)
* **TOON SPEC v3.0 §10** list-item micro-benchmark ~**6 / 17 / 23 μs** (serialize / deserialize / round-trip)
* **Large table** deserialize ~**15% faster** vs ToonLib 1.4.2; round-trip ~**36% faster** (see benchmarks; high variance on parallel encode)
* **Parallel processing** for large tables (75+ rows) and inline list arrays (200+ items) when beneficial
* **Direct JSON string support** via `JsonElement` serialization (v1.4.0)
* **POCO object serialization** via reflection (v1.4.0+)
* **60-85% faster** YAML serialization (v1.2.0) via static serializer reuse

### 🛠️ 5. CLI & Utilities

* **Command-line interface** (`toonsharp`) for file conversion
* **Validation API** for syntax checking
* **Streaming helpers** for large files
* **Formatting tools** for code style consistency

## 📦 Installation

### Via NuGet Package Manager (Recommended)

```bash
dotnet add package ToonLib --version 2.0.0
```

Or using Package Manager Console in Visual Studio:
```powershell
Install-Package ToonLib -Version 2.0.0
```

Or using NuGet CLI:
```bash
nuget install ToonLib -Version 2.0.0
```

**Package:** [ToonLib 2.0.0 on NuGet.org](https://www.nuget.org/packages/ToonLib)

### Upgrading from ToonLib 1.4.x

**2.0.0 is a breaking release.** Expect different TOON output for the same JSON input:

- List items with a tabular first field use **§10** form (`- key[N]{fields}:` on the hyphen line).
- Array fields emit **`key[N]:`** length headers.
- Tabular rows keep **comma** delimiters and **quote** cells that contain commas (no automatic `|` switch).
- More strings stay **unquoted** when safe (e.g. spaces); ambiguous tokens and structural characters are quoted per spec.
- Keys with hyphens (e.g. `build-system`) are quoted.

Pin `1.4.x` if you need byte-stable output from the pre-v3 line; adopt `2.0.0` for spec conformance and performance.

### From Source

```bash
# Clone the repository
git clone https://github.com/shinjiDev/toonsharp.git
cd toonsharp

# Build the solution
dotnet build

# Run tests
dotnet test
```

**Requirements:** .NET 10.0 or later

## 🚀 Quick Start

```csharp
using ToonSharp;

// Convert .NET object to TOON
var data = new Dictionary<string, object?>
{
    ["crew"] = new List<Dictionary<string, object?>>
    {
        new() { ["id"] = 1, ["name"] = "Luz", ["role"] = "Light glyph" },
        new() { ["id"] = 2, ["name"] = "Amity", ["role"] = "Abomination strategist" }
    },
    ["active"] = true,
    ["ship"] = new Dictionary<string, object?>
    {
        ["name"] = "Owl House",
        ["location"] = "Bonesborough"
    }
};

var toonText = Api.ToToon(data, mode: "auto");
Console.WriteLine(toonText);
// Output:
// crew[2]{id,name,role}:
//   1,Luz,"Light glyph"
//   2,Amity,"Abomination strategist"
// active: true
// ship:
//   name: "Owl House"
//   location: Bonesborough

// Convert TOON back to .NET object
var roundTrip = Api.FromToon(toonText);
// ✅ Perfect round-trip!
```

## 📖 Usage

### C# API

#### Basic Conversion

```csharp
using ToonSharp;

// JSON → TOON
var data = new Dictionary<string, object?>
{
    ["name"] = "Luz",
    ["age"] = 16,
    ["active"] = true
};
var toon = Api.ToToon(data, indent: 2, mode: "auto");

// TOON → JSON
var parsed = Api.FromToon(toon);
```

#### YAML Conversion

```csharp
// YAML → TOON
var yamlText = @"
name: Luz
age: 16
active: true
";
var toon = Api.YamlToToon(yamlText, indent: 2, mode: "auto");

// TOON → YAML
var toonText = @"
name: Luz
age: 16
active: true
";
var yaml = Api.ToonToYaml(toonText);

// Direct YAML serialization/deserialization
var data = new Dictionary<string, object?> { ["name"] = "Luz" };
var yamlOutput = Api.ToYaml(data);
var parsedData = Api.FromYaml(yamlOutput);
```

#### TOML Conversion

```csharp
// TOML → TOON (Perfect for Rust Cargo.toml, Python pyproject.toml)
var tomlText = @"
[package]
name = ""my-project""
version = ""0.1.0""

[dependencies]
serde = ""1.0""
tokio = ""1.0""
";
var toon = Api.TomlToToon(tomlText, indent: 2, mode: "auto");

// TOON → TOML
var toonText = @"
package:
  name: my-project
  version: 0.1.0
dependencies:
  serde: 1.0
  tokio: 1.0
";
var toml = Api.ToonToToml(toonText);

// Direct TOML serialization/deserialization
var data = new Dictionary<string, object?> 
{ 
    ["name"] = "my-project",
    ["version"] = "0.1.0"
};
var tomlOutput = Api.ToToml(data);
var parsedData = Api.FromToml(tomlOutput);
```

#### JSON String Conversion (v1.4.0)

```csharp
using System.Text.Json;

// Direct JSON string → TOON (via JsonElement deserialization)
var jsonString = @"{""DocumentId"":""DOC-2024-001"",""Content"":""Analysis report..."",""MaxTokens"":500,""Metrics"":[""sentiment"",""topics""]}";

// Deserialize JSON to object (returns JsonElement) and convert to TOON
var obj = JsonSerializer.Deserialize<object>(jsonString);
var toon = Api.ToToon(obj, indent: 2, mode: "auto");
// Output:
// DocumentId: DOC-2024-001
// Content: "Analysis report..."
// MaxTokens: 500
// Metrics:
//   - sentiment
//   - topics
```

#### POCO Object Serialization (v1.4.0, v1.4.1)

```csharp
// Serialize any C# class directly to TOON
// Works with or without namespace (v1.4.1 fix)
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public List<string> Tags { get; set; }
}

var person = new Person 
{ 
    Name = "Alice", 
    Age = 30, 
    Tags = new List<string> { "developer", "blogger" } 
};
var toon = Api.ToToon(person);
// Output:
// Name: John Doe
// Age: 30
// Tags[2]: developer,blogger

// Also works with anonymous types and classes without namespace
var anon = new { Title = "Report", Value = 123 };
var toonAnon = Api.ToToon(anon);
```

#### Validation

```csharp
var toonText = @"
crew[2]{id,name}:
  1,Luz
  2,Amity
";

var (isValid, errors) = Api.ValidateToon(toonText, strict: true);
if (!isValid)
{
    foreach (var error in errors)
    {
        Console.WriteLine(error);
    }
}
```

### Command-Line Interface

#### Convert JSON to TOON

```bash
dotnet run --project src/ToonSharp.CLI -- to --in data.json --out data.toon --mode readable --indent 2
```

#### Convert TOON to JSON

```bash
dotnet run --project src/ToonSharp.CLI -- from --in data.toon --out data.json --permissive
```

#### Format a TOON File

```bash
dotnet run --project src/ToonSharp.CLI -- fmt --in data.toon --out data.formatted.toon --mode readable
```

#### Convert YAML to TOON

```bash
dotnet run --project src/ToonSharp.CLI -- yaml-to-toon --in data.yaml --out data.toon --mode readable --indent 2
```

#### Convert TOON to YAML

```bash
dotnet run --project src/ToonSharp.CLI -- toon-to-yaml --in data.toon --out data.yaml --permissive
```

#### Convert TOML to TOON

```bash
dotnet run --project src/ToonSharp.CLI -- toml-to-toon --in Cargo.toml --out config.toon --mode readable --indent 2
```

#### Convert TOON to TOML

```bash
dotnet run --project src/ToonSharp.CLI -- toon-to-toml --in config.toon --out output.toml
```

**Exit Codes:**
- `0` - Success
- `2` - TOON syntax error
- `3` - General error
- `4` - I/O error

## 🧪 Testing

```bash
# Full suite (528 tests)
dotnet test -c Release

# Official spec fixtures only (358)
dotnet test -c Release --filter OfficialFixturesTests

# Published spec_v2 examples (66)
dotnet test -c Release --filter ExamplesComplianceTests

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## ⚡ Performance

ToonSharp uses **Span-based parsing**, a **`ToonWriter` serialization buffer**, and **delimiter-aware fast paths** (quoted JSON strings and unquoted comma-split inline arrays). Parallel mode remains available for large tabular encodes (75+ rows) and long inline list arrays (200+ items).

**Methodology:** **.NET 10**, **BenchmarkDotNet** `SimpleJob` (`InvocationCount=100`, `IterationCount=10` for core/YAML/TOML/JSON benches; §10 list-item uses `InvocationCount=50`, `IterationCount=8`). **ToonLib 2.0.0**, Release build, May 2026. Re-run:

```bash
dotnet build benchmarks/ToonSharp.Benchmarks/ToonSharp.Benchmarks.csproj -c Release
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release --no-build   # core + §10 (default filter)
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release --no-build -- --all --filter '*Yaml*'  # full suite (PowerShell: single-quote filter)
.\scripts\compare-vs-main-baseline.ps1   # vs pinned ToonLib 1.4.2 baselines
```

### Core TOON benchmarks (ToonLib 2.0.0)

| Operation | Size | Mean Time | Allocated | vs ToonLib 1.4.2 |
|-----------|------|-----------|-----------|------------------|
| **Serialization** | Small (~100 B) | 1.85 μs | 8.97 KB | ~same |
| | Medium (~1 KB) | 11.02 μs | 10.38 KB | ~same |
| | Large (~10 KB) | 84.6 μs | 45.18 KB | **~−32%** |
| | Large Table (200 rows) | 498.1 μs | 384.28 KB | ~same † |
| | Large Array (1000 inline items) | 212.3 μs | 277.48 KB | **~−43%** |
| **Deserialization** | Small | 9.30 μs | 2.02 KB | ~same |
| | Medium | 29.78 μs | 7.06 KB | ~same |
| | Large | 197.3 μs | 72.14 KB | ~−14% |
| | Large Table (200 rows) | 341.5 μs | 362.52 KB | **~−15%** |
| | Large Array (1000 inline items) | 149.3 μs | 171.22 KB | **~−66%** |
| **Round-Trip** | Small | 10.67 μs | 10.98 KB | ~same |
| | Medium | 41.59 μs | 17.44 KB | ~−19% |
| | Large | 324.9 μs | 117.32 KB | ~+25% †† |
| | Large Table (200 rows) | 582.9 μs | 749.46 KB | **~−36%** † |
| | Large Array (1000 inline items) | 317.2 μs | 448.69 KB | **~−62%** |

† `Serialize_LargeTable` / `RoundTrip_LargeTable` show high variance in BenchmarkDotNet (parallel row encoding); compare script uses the same pinned methodology as other large workloads.

†† `RoundTrip_Large` regressed vs 1.4.2 on this workload (nested non-tabular ~10 KB object); large-array and large-table paths are the primary v2.0.0 wins.

**TOON SPEC v3.0 §10 list-item (tabular on hyphen line):**

| Operation | Mean Time | Allocated |
|-----------|-----------|-----------|
| Serialize | 5.93 μs | 9.45 KB |
| Deserialize | 16.94 μs | 4.19 KB |
| Round-trip | 22.66 μs | 13.63 KB |

### vs ToonLib 1.4.2 — headline (parallel workloads)

| Method | 1.4.2 (μs) | 2.0.0 (μs) | Delta |
|--------|------------|------------|-------|
| `Deserialize_LargeArray` | 435 | **147** | **~−66%** |
| `Serialize_LargeArray` | 373 | **211** | **~−43%** |
| `RoundTrip_LargeArray` | 830 | **319** | **~−62%** |
| `Deserialize_LargeTable` | 403 | **342** | **~−15%** |
| `Serialize_LargeTable` | 512 | **498** | ~same |
| `RoundTrip_LargeTable` | 913 | **583** | **~−36%** |

**Notes:**
- **Large array deserialize** improved via unquoted comma-split fast path (no `"` in payload).
- **Large array serialize** emits one inline `items[1000]: …` line — fewer tokens, single buffer growth.
- Historical mid-2.0.0 branch numbers (pre fast-path): [README.v2.md](README.v2.md).

### YAML conversion (ToonLib 2.0.0)

Measured with the same BenchmarkDotNet job as core TOON benches (`--all --filter *YamlBenchmarks*`).

| Operation | Size | Mean Time | Allocated |
|-----------|------|-----------|-----------|
| **YAML → TOON** (`YamlToToon`) | Small (~100 B) | 42.16 μs | 22.95 KB |
| | Medium (~1 KB) | 311.40 μs | 60.45 KB |
| | Large (~10 KB) | 313.34 μs | 342.02 KB |
| **TOON → YAML** (`ToonToYaml`) | Small | 45.08 μs | 19.52 KB |
| | Medium | 252.96 μs | 41.93 KB |
| | Large | 508.79 μs | 227.13 KB |
| **YAML serialize** (`ToYaml`) | Small | 34.13 μs | 17.50 KB |
| | Medium | 130.05 μs | 34.98 KB |
| | Large | 291.33 μs | 155.84 KB |
| **YAML deserialize** (`FromYaml`) | Small | 35.62 μs | 13.97 KB |
| | Medium | 272.35 μs | 49.58 KB |
| | Large | 289.34 μs | 300.77 KB |
| **YAML round-trip** | Small | 90.61 μs | 42.24 KB |
| | Medium | 637.83 μs | 103.93 KB |
| | Large | 554.45 μs | 568.85 KB |

Uses static YamlDotNet serializer instances (since v1.2.0).

### TOML conversion (ToonLib 2.0.0)

Measured with `--all --filter *TomlBenchmarks*` (Tomlyn).

| Operation | Size | Mean Time | Allocated |
|-----------|------|-----------|-----------|
| **TOML → TOON** (`TomlToToon`) | Small (~100 B) | 7.99 μs | 18.90 KB |
| | Medium (~1 KB) | 30.49 μs | 48.77 KB |
| | Large (~10 KB) | 376.92 μs | 461.32 KB |
| **TOON → TOML** (`ToonToToml`) | Small | 3.23 μs | 5.55 KB |
| | Medium | 10.89 μs | 18.74 KB |
| | Large | 121.59 μs | 193.70 KB |
| **TOML serialize** (`ToToml`) | Small | 1.65 μs | 3.56 KB |
| | Medium | 5.25 μs | 10.48 KB |
| | Large | 58.01 μs | 109.33 KB |
| **TOML deserialize** (`FromToml`) | Small | 6.30 μs | 10.05 KB |
| | Medium | 26.62 μs | 37.08 KB |
| | Large | 334.32 μs | 419.33 KB |
| **TOML round-trip** | Small | 11.09 μs | 17.17 KB |
| | Medium | 39.75 μs | 58.03 KB |
| | Large | 482.13 μs | 638.01 KB |

TOON → TOML remains much faster than TOON → YAML for typical config sizes.

### JSON and POCO serialization (ToonLib 2.0.0)

`Dictionary<string, object?>` is the baseline (ratio 1.00). `--all --filter *JsonElementBenchmarks*` / `*PocoBenchmarks*`.

#### JsonElement and JSON string → TOON

| Operation | Size | Mean Time | Allocated | vs Dictionary |
|-----------|------|-----------|-----------|---------------|
| **JsonElement → TOON** | Small (~100 B) | 6.29 μs | 9.67 KB | 2.93× |
| | Medium (~1 KB) | 17.01 μs | 13.69 KB | 7.69× |
| | Large (~10 KB) | 209.75 μs | 263.36 KB | 98× |
| **JSON string → TOON** | Small | 9.60 μs | 9.99 KB | 4.35× |
| | Medium | 44.38 μs | 14.70 KB | 20.77× |
| | Large | 271.03 μs | 298.72 KB | 125× |
| **Dictionary (baseline)** | Small | 2.19 μs | 8.97 KB | 1.00× |
| | Medium | 10.36 μs | 11.40 KB | 4.77× |
| | Large | 151.66 μs | 157.12 KB | 70× |

#### POCO and anonymous types → TOON

| Operation | Size | Mean Time | Allocated | vs Dictionary |
|-----------|------|-----------|-----------|---------------|
| **POCO → TOON** | Small (4 props) | 8.97 μs | 9.70 KB | 4.29× |
| | Medium (~10 props) | 23.45 μs | 13.24 KB | 11.29× |
| | Large (100 objects) | 491.03 μs | 240.26 KB | 250× |
| **Anonymous → TOON** | Small | 6.64 μs | 9.74 KB | 3.21× |
| | Medium | 10.67 μs | 10.59 KB | 5.10× |
| **Dictionary (baseline)** | Small | 2.15 μs | 8.96 KB | 1.00× |
| | Medium | 10.59 μs | 11.40 KB | 5.06× |
| | Large | 154.81 μs | 157.11 KB | 74× |

Reflection-based POCO paths add overhead; prefer dictionaries or `JsonElement` for hot paths.

### Running Benchmarks

```bash
# Core TOON + v3 §10 benchmarks (recommended, ~30s)
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release

# Full suite (YAML, TOML, POCO, thresholds, etc.)
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release -- --all

# Or use the helper script
.\scripts\run-core-benchmarks.ps1
```

Reports are written to `BenchmarkDotNet.Artifacts/results/` (Markdown, HTML, CSV). To compare against a saved baseline:

```powershell
# Compare Large Array / Large Table vs v1.4.2 baselines
.\scripts\compare-vs-main-baseline.ps1

# Optional v3 iteration history
.\scripts\compare-v3-benchmarks.ps1
```

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[README.md](README.md)** (this file) | **ToonLib 2.0.0** + **TOON SPEC v3.0** — default |
| **[README.v2.md](README.v2.md)** | **TOON SPEC v2** example corpus notes (not library 1.4.x) |
| **`docs/spec_summary.md`** | Concise spec overview with ABNF notes |
| **`docs/examples.md`** | JSON⇄TOON conversion examples |
| **`docs/assumptions.md`** | Strict vs permissive behavior and documented edge cases |
| **`tests/fixtures/spec/`** | Official encode/decode conformance JSON |
| **`examples/spec_v2/`** | Published spec example corpus |

## 🌟 Use Cases

* **Data Serialization**: Efficient storage and transmission of structured data
* **API Development**: Lightweight data format for REST APIs
* **Configuration Files**: Human-readable config format with comments support
* **Data Pipelines**: Stream processing of large JSON/YAML/TOML datasets
* **ML/AI Projects**: Token-optimized format for LLM training data
* **Format Migration**: Convert between JSON, YAML, TOML, and TOON seamlessly
* **DevOps & Infrastructure**: Transform configuration files between different formats
* **Cross-Ecosystem Development**: Convert Rust Cargo.toml ↔ Python pyproject.toml ↔ .NET configs

## 📖 Examples & conformance

**Official fixtures** (`tests/fixtures/spec/`, 358 tests via `OfficialFixturesTests`):

- `encode/` — expected TOON output for JSON inputs (delimiters, key folding, §10 arrays, primitives, …).
- `decode/` — expected JSON for TOON inputs (validation errors, whitespace, path expansion, …).

**Published examples** (`examples/spec_v2/`, 66 tests via `ExamplesComplianceTests`):

- `conversions/` — JSON ↔ TOON pairs from the [spec repository](https://github.com/toon-format/spec/tree/main/examples).
- `valid/` / `invalid/` — canonical and error cases for strict mode.

```bash
dotnet test -c Release --filter OfficialFixturesTests
dotnet test -c Release --filter ExamplesComplianceTests
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

**Guidelines:**
- Follow C# coding conventions
- Add tests for new features
- Update documentation as needed
- Ensure all tests pass: `dotnet test`
- Keep additions aligned with [TOON SPEC v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md)

## 📋 Release Notes

### ToonLib 2.0.0 (2026-05) — **current** · implements TOON SPEC v3.0

Major release after **1.4.x**. Breaking changes to encoded TOON; decode aligned with the official spec test corpora.

**Spec conformance:**
- **358** official encode/decode fixtures (`tests/fixtures/spec/`) + **66** `examples/spec_v2/` cases — **528** total tests.
- §10 list-item encoding/decoding; `key[N]:` headers; §11 comma + quoted tabular cells; §7.2/§7.3 quoting rules.
- Parser: strict blank lines, `- [N]:` vs bracket-only lines, JSON root scalars, `expandPaths` defaults.

**Performance (vs ToonLib 1.4.2 on reference benchmarks):**
- `ToonWriter` / `AppendScalar` serialization buffer.
- Unquoted **comma-split inline array** decode fast path.
- Large array: **~−66%** deserialize, **~−43%** serialize, **~−62%** round-trip (Release, May 2026).

**Documentation:**
- Default [README.md](README.md) for **library 2.0.0** + **SPEC v3.0**.
- [README.v2.md](README.v2.md) for the **TOON SPEC v2** published example set.

### v1.4.2 (2024-12-05)
- **Bugfix**: Fixed root-level POCO list serialization to use tabular format
- `Api.ToToon(listOfPocos)` now correctly outputs tabular format `[N]{fields}:`
- Added `TryWriteRootArrayAsTabular` for proper array detection at root level

### v1.4.1 (2024-12-05)
- **Bugfix**: Fixed POCO serialization for classes without namespace
- Classes defined without a `namespace` declaration now serialize correctly to TOON
- Improved reflection logic for detecting serializable types

### v1.4.0 (2024-12-04)
- **New**: Direct `JsonElement` serialization support
- **New**: POCO object serialization via reflection
- **New**: Anonymous type serialization support

### v1.3.0 (2024-12-03)
- **New**: TOML ↔ TOON bidirectional conversion
- **New**: CLI commands for TOML conversion

### v1.2.0 (2024-12-02)
- **New**: YAML ↔ TOON bidirectional conversion
- **Performance**: 60-85% faster YAML serialization with static instance reuse

### v1.1.0 (2024-12-01)
- **Performance**: 40-46% faster large table operations
- **Performance**: Parallel processing for large datasets

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Christian Palomares** - [@shinjidev](https://github.com/shinjidev)

## ☕ Support

If you find this project helpful, consider supporting my work:

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-FFDD00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/shinjidev)

## 🙏 Acknowledgments

* **ToonLib 2.0.0** built for [TOON SPEC v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md) (SPEC v2 examples: [README.v2.md](README.v2.md))
* Inspired by the need for efficient, token-optimized data serialization
* C# implementation inspired by the original TOON reference tooling

---

⭐ **Star this repository if you find it useful!** ⭐

