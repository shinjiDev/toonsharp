# 🔄 ToonSharp (C#)

[![NuGet Version](https://img.shields.io/nuget/v/ToonLib.svg?label=NuGet&color=blue)](https://www.nuget.org/packages/ToonLib)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ToonLib.svg?label=Downloads&color=green)](https://www.nuget.org/packages/ToonLib)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)](https://github.com/shinjiDev/toonsharp/actions)

A production-grade C# library and CLI that converts data between JSON, YAML, TOML, and TOON (Token-Oriented Object Notation) while fully conforming to **TOON SPEC v3.0**. Perfect for .NET developers and data engineers who need efficient, token-optimized data serialization.

**✅ TOON SPEC v3.0 (default)** — Canonical §10 list-item encoding, array length headers (`key[N]:`), official encode/decode fixtures, and span-optimized I/O. For v2-focused notes and historical benchmarks, see **[README.v2.md](README.v2.md)**.

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
* **Unquoted inline-array fast path** — comma-split decode for `key[N]: a,b,c` without quotes (~**35% faster** large-array deserialize vs v1.4.2; see benchmarks)
* **`ToonWriter` + `AppendScalar`** — serialize directly into a shared buffer (large inline arrays **~40% faster** serialize vs v1.4.2)
* **TOON SPEC v3.0 §10** list-item micro-benchmark ~**9 / 17 / 25 μs** (serialize / deserialize / round-trip)
* **Large table** serialize/deserialize ~**20–30% faster** vs v1.4.2 (stricter tabular eligibility + row-indent parsing)
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
dotnet add package ToonLib
```

Or using Package Manager Console in Visual Studio:
```powershell
Install-Package ToonLib
```

Or using NuGet CLI:
```bash
nuget install ToonLib
```

**Package:** [ToonLib on NuGet.org](https://www.nuget.org/packages/ToonLib)

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

**Requirements:** .NET 9.0 or later

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

Benchmarks: **.NET 9**, **BenchmarkDotNet** short job (`--warmupCount 1 --iterationCount 5`, `InvocationCount=100` for large workloads). Baseline **v1.4.2** = `main` on the same machine (`scripts/compare-vs-main-baseline.ps1`). Re-run after `dotnet build -c Release`.

### Core TOON benchmarks (current branch)

| Operation | Size | Mean Time | Allocated | vs v1.4.2 |
|-----------|------|-----------|-----------|-----------|
| **Serialization** | Small (~100 B) | 2.16 μs | 8.96 KB | ~same |
| | Medium (~1 KB) | 10.25 μs | 10.38 KB | ~same |
| | Large (~10 KB) | 89.80 μs | 45.19 KB | ~−35% |
| | Large Table (200 rows) | 359.7 μs | 403.61 KB | **~−30%** |
| | Large Array (1000 inline items) | 378.1 μs | 277.48 KB | **~+1%** † |
| **Deserialization** | Small | 10.84 μs | 2.02 KB | ~same |
| | Medium | 30.76 μs | 7.06 KB | ~same |
| | Large | 237.7 μs | 72.14 KB | ~−8% |
| | Large Table (200 rows) | 329.6 μs | 370.35 KB | **~−18%** |
| | Large Array (1000 inline items) | 281.0 μs | 171.22 KB | **~−35%** |
| **Round-Trip** | Small | 14.29 μs | 10.98 KB | ~same |
| | Medium | 39.42 μs | 17.44 KB | ~−4% |
| | Large | 355.9 μs | 117.32 KB | **~−19%** |
| | Large Table (200 rows) | 849.8 μs | 725.97 KB | **~−7%** |
| | Large Array (1000 inline items) | 526.6 μs | 448.69 KB | **~−37%** |

† Large-array **serialize** is often **~220 μs** (~**−41%** vs v1.4.2) when the buffer is warm; short-job runs vary with allocation noise.

**v3.0 §10 list-item (tabular on hyphen line):**

| Operation | Mean Time | Allocated |
|-----------|-----------|-----------|
| Serialize | 9.29 μs | 9.45 KB |
| Deserialize | 17.32 μs | 4.19 KB |
| Round-trip | 24.53 μs | 13.63 KB |

### vs v1.4.2 — headline deltas (Large Array, 1000 items)

| Method | v1.4.2 (μs) | Current (μs) | Delta |
|--------|-------------|--------------|-------|
| `Deserialize_LargeArray` | 435 | **281** | **~−35%** |
| `Serialize_LargeArray` | 373 | **218–378** | **~−17% to −41%** |
| `RoundTrip_LargeArray` | 830 | **344–527** | **~−37% to −59%** |

**Notes:**
- **vs v1.4.2** uses pinned baselines in `scripts/compare-vs-main-baseline.ps1` (update after intentional perf work).
- **Large array deserialize** improved via unquoted comma-split fast path (no `"` in payload).
- **Large array serialize** emits one inline `items[1000]: …` line — fewer tokens, single buffer growth.
- **Large table** benefits from stricter tabular detection and comma-row writer.
- Historical v2.0.0-era numbers: [README.v2.md](README.v2.md).

### YAML Conversion Performance

**🚀 Version 1.2.0 YAML Performance Improvements:**

ToonSharp v1.2.0 introduces significant YAML performance optimizations through static serializer/deserializer instance reuse:

| Operation | Size | Mean Time | Improvement vs v1.1.0 | Allocated Memory |
|-----------|------|-----------|----------------------|------------------|
| **YAML → TOON** | Small (~100 B) | 44.83 μs | **26% faster** | 14.76 KB |
| | Medium (~1 KB) | 367.26 μs | **9% faster** | 51.78 KB |
| | Large (~10 KB) | 364.67 μs | **15% faster** | 335.23 KB |
| **TOON → YAML** | Small | 44.89 μs | **79% faster** 🚀 | 18.51 KB |
| | Medium | 202.42 μs | **61% faster** 🚀 | 43.8 KB |
| | Large | 274.42 μs | **85% faster** 🚀 | 223.01 KB |
| **YAML Serialization** | Small | 34.35 μs | **74% faster** 🚀 | 16.66 KB |
| | Medium | 145.16 μs | **61% faster** 🚀 | 34.15 KB |
| | Large | 202.06 μs | **80% faster** 🚀 | 155.03 KB |
| **YAML Deserialization** | Small | 39.77 μs | **36% faster** 🚀 | 13.85 KB |
| | Medium | 320.79 μs | **26% faster** | 48.88 KB |
| | Large | 307.28 μs | **4% faster** | 297.91 KB |
| **YAML Round-Trip** | Small | 91.75 μs | **68% faster** 🚀 | 33.26 KB |
| | Medium | 724.83 μs | **23% faster** | 95.57 KB |
| | Large | 695.73 μs | **18% faster** | 558.3 KB |

**YAML Performance Notes:**
- v1.2.0 achieves **60-85% faster** YAML serialization through static instance reuse
- YAML conversion leverages the YamlDotNet library for robust YAML support
- Deserialization improvements are more modest (4-36%) due to parser overhead
- Memory allocation significantly reduced across all operations

### TOML Conversion Performance

**🎯 Version 1.3.0 TOML Performance:**

ToonSharp v1.3.0 introduces TOML support with excellent performance characteristics using the Tomlyn library:

| Operation | Size | Mean Time | Allocated Memory |
|-----------|------|-----------|------------------|
| **TOML → TOON** | Small (~100 B) | 9.09 μs | 10.77 KB |
| | Medium (~1 KB) | 35.84 μs | 42.12 KB |
| | Large (~10 KB) | 435.12 μs | 493.24 KB |
| **TOON → TOML** | Small | 3.26 μs | 5.36 KB |
| | Medium | 12.50 μs | 18.35 KB |
| | Large | 172.30 μs | 216.85 KB |
| **TOML Serialization** | Small | 2.20 μs | 3.56 KB |
| | Medium | 5.99 μs | 10.48 KB |
| | Large | 68.40 μs | 109.33 KB |
| **TOML Deserialization** | Small | 6.56 μs | 10.05 KB |
| | Medium | 28.46 μs | 37.08 KB |
| | Large | 347.66 μs | 419.37 KB |
| **TOML Round-Trip** | Small | 11.96 μs | 17.17 KB |
| | Medium | 42.82 μs | 58.04 KB |
| | Large | 502.88 μs | 638.07 KB |

**TOML Performance Notes:**
- **Excellent serialization speed**: TOON → TOML is 2-4x faster than YAML serialization
- **Efficient memory usage**: Lower memory allocation compared to YAML operations
- **Ideal for configuration files**: Perfect for Rust Cargo.toml, Python pyproject.toml
- Leverages the high-performance Tomlyn library for TOML parsing
- Optimized for typical configuration file sizes (small to medium)

### JSON & Object Serialization Performance

**🚀 Version 1.4.0 JsonElement & POCO Support:**

ToonSharp v1.4.0 introduces direct serialization support for `JsonElement` (from `System.Text.Json`) and POCO objects via reflection:

#### JsonElement Serialization (JSON String → TOON)

| Operation | Size | Mean Time | Allocated Memory | Ratio vs Dictionary |
|-----------|------|-----------|------------------|---------------------|
| **JsonElement → TOON** | Small (~100 B) | 4.24 μs | 1.72 KB | 1.27x |
| | Medium (~1 KB) | 14.78 μs | 7.73 KB | 4.30x |
| | Large (~10 KB) | 423.17 μs | 293.76 KB | 123.18x |
| **JSON String → TOON** | Small | 11.36 μs | 2.05 KB | 3.32x |
| | Medium | 33.83 μs | 8.75 KB | 9.84x |
| | Large | 630.05 μs | 321.18 KB | 183.79x |
| **Dictionary (Baseline)** | Small | 3.63 μs | 1.01 KB | 1.00x |
| | Medium | 9.66 μs | 5.38 KB | 2.79x |
| | Large | 351.34 μs | 185.23 KB | 99.92x |

#### POCO Object Serialization (C# Class → TOON)

| Operation | Size | Mean Time | Allocated Memory | Ratio vs Dictionary |
|-----------|------|-----------|------------------|---------------------|
| **POCO → TOON** | Small (4 props) | 12.42 μs | 1.70 KB | 3.49x |
| | Medium (~10 props) | 24.20 μs | 7.09 KB | 6.45x |
| | Large (100 objects) | 535.97 μs | 248.92 KB | 142.22x |
| **Anonymous Type → TOON** | Small | 11.35 μs | 1.80 KB | 2.93x |
| | Medium | 13.57 μs | 3.62 KB | 3.69x |
| **Dictionary (Baseline)** | Small | 3.90 μs | 1.01 KB | 1.00x |
| | Medium | 10.04 μs | 5.38 KB | 2.70x |
| | Large | 345.43 μs | 184.51 KB | 93.61x |

**JsonElement & POCO Performance Notes:**
- **JsonElement near-native speed**: Only 1.27x overhead vs pre-built dictionaries for small objects
- **POCO serialization via reflection**: Serialize any C# class directly to TOON
- **Anonymous types support**: Works with `new { ... }` syntax
- **2-4x overhead for typical use cases**: Excellent performance for most scenarios

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
| **[README.md](README.md)** (this file) | **TOON SPEC v3.0** — default |
| **[README.v2.md](README.v2.md)** | v2-oriented notes and v2.0.0-era benchmarks |
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

### v3.0 (current) — TOON SPEC v3.0 + performance

**Spec conformance:**
- **358** official encode/decode fixtures (`tests/fixtures/spec/`) + **66** `examples/spec_v2/` cases — **528** total tests.
- §10 list-item encoding/decoding; `key[N]:` headers; §11 comma + quoted tabular cells; §7.2/§7.3 quoting rules.
- Parser fixes: strict blank lines, `- [N]:` vs bracket-only lines, JSON root scalars, `expandPaths` defaults.

**Performance:**
- `ToonWriter` / `AppendScalar` serialization path.
- Unquoted **comma-split inline array** decode fast path.
- Large array: **~−35%** deserialize, **~−37%** round-trip vs v1.4.2 (typical Release build).

**Docs:**
- Default **README.md** = v3; legacy **README.v2.md** for v2 context.

### v2.0.0 (2026-05-16) — TOON SPEC v3.0 encoding (see [README.v2.md](README.v2.md))

**Breaking changes (encode):**
- Conformant **§10** encoding: list-item objects with a tabular first field use `- key[N]{fields}:` on the hyphen line; rows at depth +2.
- Array fields emit length headers: `key[N]:` (and inline primitive arrays where applicable).

**Improvements:**
- Stricter **tabular detection** (primitive-only values).
- Parser: row-level indent for tabular blocks; §10 list-item decode.
- Large table serialize/deserialize ~20–24% faster vs v1.4.2 on reference benchmarks.

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

* Built following [TOON SPEC v3.0](https://github.com/toon-format/spec/blob/main/SPEC.md) (v2 notes: [README.v2.md](README.v2.md))
* Inspired by the need for efficient, token-optimized data serialization
* C# implementation inspired by the original TOON reference tooling

---

⭐ **Star this repository if you find it useful!** ⭐

