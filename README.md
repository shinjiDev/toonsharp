# 🔄 ToonSharp (C#)

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)](https://github.com/shinjiDev/toonsharp/actions)

A production-grade C# library and CLI that converts data between JSON and TOON (Token-Oriented Object Notation) while fully conforming to **TOON SPEC v2.0**. Perfect for .NET developers and data engineers who need efficient, token-optimized data serialization.

**✅ Full TOON SPEC v2.0 Compliance** - This library implements all examples from the [official TOON specification repository](https://github.com/toon-format/spec/tree/main/examples), ensuring complete compatibility with the standard.

## ✨ Features

The `ToonSharp` library provides comprehensive JSON ↔ TOON conversion capabilities:

### 🔧 1. Lossless Conversion

* **Bidirectional conversion** between JSON-compatible .NET objects and TOON text
* **Round-trip preservation** - data integrity guaranteed
* Supports all JSON data types (objects, arrays, scalars)
* Handles nested structures of any depth

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

* **Parallel processing** for large tables (50+ rows) and arrays (200+ items)
* **Span<T> optimizations** for zero-allocation string operations
* **Memory-efficient** parsing and serialization
* **Automatic threshold tuning** based on data size

### 🛠️ 5. CLI & Utilities

* **Command-line interface** (`toonsharp`) for file conversion
* **Validation API** for syntax checking
* **Streaming helpers** for large files
* **Formatting tools** for code style consistency

## 📦 Installation

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

The following benchmarks were executed on **.NET 9.0** with BenchmarkDotNet:

### Performance Results

| Operation | Size | Mean Time | Std Deviation | Allocated Memory |
|-----------|------|-----------|--------------|------------------|
| **Serialization** (JSON → TOON) | Small (~100 B) | 7.91 μs | ±3.33 μs | 998 B |
| | Medium (~1 KB) | 30.54 μs | ±9.04 μs | 4,924 B |
| | Large (~10 KB) | 357.47 μs | ±44.24 μs | 53,702 B |
| | Large Table (200 rows) | 854.13 μs | ±69.35 μs | 340,141 B |
| | Large Array (1000 items) | 673.38 μs | ±138.29 μs | 788,061 B |
| **Deserialization** (TOON → JSON) | Small | 10.04 μs | ±1.12 μs | 5,399 B |
| | Medium | 31.16 μs | ±3.22 μs | 23,689 B |
| | Large | 476.43 μs | ±51.16 μs | 284,256 B |
| | Large Table (200 rows) | 1,026.56 μs | ±221.65 μs | 1,169,815 B |
| | Large Array (1000 items) | 659.95 μs | ±44.71 μs | 1,139,510 B |
| **Round-Trip** (JSON → TOON → JSON) | Small | 16.64 μs | ±3.71 μs | 6,381 B |
| | Medium | 52.25 μs | ±10.76 μs | 28,612 B |
| | Large | 632.71 μs | ±215.92 μs | 337,964 B |
| | Large Table (200 rows) | 1,486.09 μs | ±358.55 μs | 1,436,083 B |
| | Large Array (1000 items) | 1,333.16 μs | ±123.21 μs | 1,861,480 B |

**Notes:**
- Benchmarks run in Release mode with full optimizations
- Times include GC overhead and memory allocation
- Results may vary based on hardware and system load
- Large Table and Large Array benchmarks use parallel processing (50+ rows, 200+ items)

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
* **Data Pipelines**: Stream processing of large JSON datasets
* **ML/AI Projects**: Token-optimized format for LLM training data

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

