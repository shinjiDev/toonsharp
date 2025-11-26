# 🔄 ToonSharp (C#)

[![NuGet Version](https://img.shields.io/nuget/v/ToonLib.svg?label=NuGet&color=blue)](https://www.nuget.org/packages/ToonLib)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ToonLib.svg?label=Downloads&color=green)](https://www.nuget.org/packages/ToonLib)
[![.NET Version](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)](https://github.com/shinjiDev/toonsharp/actions)

A production-grade C# library and CLI that converts data between JSON, YAML, and TOON (Token-Oriented Object Notation) while fully conforming to **TOON SPEC v2.0**. Perfect for .NET developers and data engineers who need efficient, token-optimized data serialization.

**✅ Full TOON SPEC v2.0 Compliance** - This library implements all examples from the [official TOON specification repository](https://github.com/toon-format/spec/tree/main/examples), ensuring complete compatibility with the standard.

## ✨ Features

The `ToonSharp` library provides comprehensive JSON ↔ TOON ↔ YAML conversion capabilities:

### 🔧 1. Lossless Conversion

* **Bidirectional conversion** between JSON, YAML, and TOON formats
* **Round-trip preservation** - data integrity guaranteed
* Supports all JSON/YAML data types (objects, arrays, scalars)
* Handles nested structures of any depth
* **YAML support** - Convert YAML ↔ TOON seamlessly

### 📊 2. Advanced Parser & Lexer

* **Recursive descent parser** with indentation tracking
* **Comment support** - inline (`#`, `//`) and block (`/* */`) comments
* **ABNF-backed grammar** - fully compliant with TOON SPEC v2.0
* **Error reporting** with line and column numbers

### 🚀 3. Automatic Tabular Detection

* **Smart detection** of uniform-object arrays
* **Automatic emission** of efficient tabular mode (`key[N]{fields}:`)
* **Token savings estimation**
* **Configurable modes**: auto, compact, readable

### ⚡ 4. Performance Optimizations

* **60-85% faster** YAML serialization (v1.2.0) 🚀
* **40-46% faster** for large table operations (v1.1.0)
* **16-20% faster** for large array deserialization
* **Parallel processing** for large tables (50+ rows) and arrays (200+ items)
* **Span<T> optimizations** for zero-allocation string operations
* **Static instance reuse** for YAML serializers/deserializers
* **Memory-efficient** parsing and serialization
* **Automatic threshold tuning** based on data size

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

**Exit Codes:**
- `0` - Success
- `2` - TOON syntax error
- `3` - General error
- `4` - I/O error

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## ⚡ Performance

ToonSharp is optimized for high performance using **parallel processing** and **Span<T>** optimizations. The library automatically uses parallel processing for large datasets (tables with 50+ rows, arrays with 200+ items) and leverages `Span<T>` for zero-allocation string operations, significantly reducing memory allocations and improving throughput.

**🚀 Version 1.1.0 Performance Improvements:**
- **40-46% faster** for large table operations (deserialization and round-trip)
- **16-20% faster** for large array deserialization
- Optimized `IterLines` with Span-based line processing
- Optimized `ParseValue` with simplified comparisons and caching

The following benchmarks were executed on **.NET 9.0** with BenchmarkDotNet (v1.1.0):

### Performance Results

| Operation | Size | Mean Time | Std Deviation | Allocated Memory |
|-----------|------|-----------|--------------|------------------|
| **Serialization** (JSON → TOON) | Small (~100 B) | 9.043 μs | ±0.43 μs | 1,010 B |
| | Medium (~1 KB) | 29.068 μs | ±2.00 μs | 3,442 B |
| | Large (~10 KB) | 325.625 μs | ±23.10 μs | 43,042 B |
| | Large Table (200 rows) | 606.985 μs | ±126.06 μs | 318,352 B |
| | Large Array (1000 items) | 529.001 μs | ±21.82 μs | 737,500 B |
| **Deserialization** (TOON → JSON) | Small | 12.043 μs | ±1.11 μs | 1,899 B |
| | Medium | 44.646 μs | ±3.43 μs | 10,011 B |
| | Large | 400.055 μs | ±8.17 μs | 70,515 B |
| | Large Table (200 rows) | 611.939 μs | ±52.06 μs | 476,681 B |
| | Large Array (1000 items) | 438.117 μs | ±12.35 μs | 350,435 B |
| **Round-Trip** (JSON → TOON → JSON) | Small | 25.491 μs | ±1.95 μs | 2,895 B |
| | Medium | 72.087 μs | ±2.29 μs | 13,442 B |
| | Large | 660.590 μs | ±441.61 μs | 113,546 B |
| | Large Table (200 rows) | 799.313 μs | ±186.01 μs | 805,424 B |
| | Large Array (1000 items) | 812.023 μs | ±16.82 μs | 931,439 B |

**Notes:**
- Benchmarks run in Release mode with full optimizations
- Times include GC overhead and memory allocation
- Results may vary based on hardware and system load
- Large Table and Large Array benchmarks use parallel processing (50+ rows, 200+ items)
- Performance improvements in v1.1.0: 40-46% faster for large table operations compared to v1.0.0

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

### Running Benchmarks

```bash
# Run all benchmarks
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release

# Run specific benchmarks
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release -- --filter "*Small*"
```

Complete results are automatically exported to `BenchmarkDotNet.Artifacts/results/` in Markdown, HTML, and CSV formats.

## 📚 Documentation

Comprehensive documentation is available in the `docs/` directory:

- **`docs/spec_summary.md`** – Concise TOON SPEC v2.0 overview with ABNF notes
- **`docs/examples.md`** – JSON⇄TOON conversion examples
- **`docs/assumptions.md`** – Documented gaps/assumptions + strict vs. permissive behavior

## 🌟 Use Cases

* **Data Serialization**: Efficient storage and transmission of structured data
* **API Development**: Lightweight data format for REST APIs
* **Configuration Files**: Human-readable config format with comments support
* **Data Pipelines**: Stream processing of large JSON/YAML datasets
* **ML/AI Projects**: Token-optimized format for LLM training data
* **Format Migration**: Convert between JSON, YAML, and TOON seamlessly
* **DevOps**: Transform configuration files between different formats

## 📖 Examples

The `examples/spec_v2/` directory contains all material from the official [`toon-format/spec`](https://github.com/toon-format/spec/tree/main/examples) repository:

- `conversions/` – JSON ↔ TOON pairs published by the specification.
- `valid/` – all canonical examples (key folding, custom delimiters, primitive arrays, etc.).
- `invalid/` – edge cases that must fail in strict mode.
- `basic_object`, `tabular_array`, `mixed_structures` – ToonSharp-specific examples designed for quick documentation.

The `ExamplesComplianceTests` test suite iterates through **every** official TOON file and verifies that:

1. Valid examples can be parsed, validated, and round-tripped without loss.
2. JSON ↔ TOON pairs remain equivalent after serializing/deserializing with ToonSharp.
3. Invalid examples throw `ToonSyntaxError` in strict mode.

```bash
dotnet test --filter ExamplesComplianceTests
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
- Keep additions aligned with TOON SPEC v2.0

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👨‍💻 Author

**Christian Palomares** - [@shinjidev](https://github.com/shinjidev)

## ☕ Support

If you find this project helpful, consider supporting my work:

[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-FFDD00?style=for-the-badge&logo=buy-me-a-coffee&logoColor=black)](https://www.buymeacoffee.com/shinjidev)

## 🙏 Acknowledgments

* Built following [TOON SPEC v2.0](https://github.com/toon-format/spec)
* Inspired by the need for efficient, token-optimized data serialization
* C# implementation inspired by the original TOON reference tooling

---

⭐ **Star this repository if you find it useful!** ⭐

