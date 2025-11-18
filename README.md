# 🔄 ToonSharp (C#)

[![.NET Version](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen.svg)](https://github.com/shinjidev/toonsharp-csharp/actions)

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

### 🛠️ 4. CLI & Utilities

* **Command-line interface** (`toonsharp`) for file conversion
* **Validation API** for syntax checking
* **Streaming helpers** for large files
* **Formatting tools** for code style consistency

## 📦 Installation

```bash
# Clone the repository
git clone https://github.com/shinjidev/toonsharp-csharp.git
cd toonsharp-csharp

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

ToonSharp está optimizado para alto rendimiento. Los siguientes benchmarks fueron ejecutados en **.NET 9.0** con BenchmarkDotNet:

### Resultados de Rendimiento

| Operación | Tamaño | Tiempo Medio | Desviación Estándar | Memoria Asignada |
|-----------|--------|--------------|---------------------|------------------|
| **Serialización** (JSON → TOON) | Pequeño (~100 B) | 9.26 μs | ±4.54 μs | 996 B |
| | Mediano (~1 KB) | 27.51 μs | ±10.72 μs | 4,737 B |
| | Grande (~10 KB) | 298.08 μs | ±70.34 μs | 53,164 B |
| **Deserialización** (TOON → JSON) | Pequeño | 11.27 μs | ±4.32 μs | 5,488 B |
| | Mediano | 43.92 μs | ±11.38 μs | 28,704 B |
| | Grande | 448.82 μs | ±54.07 μs | 314,278 B |
| **Round-Trip** (JSON → TOON → JSON) | Pequeño | 22.41 μs | ±7.12 μs | 6,476 B |
| | Mediano | 66.35 μs | ±17.12 μs | 33,427 B |
| | Grande | 714.72 μs | ±97.43 μs | 367,434 B |

**Notas:**
- Los benchmarks se ejecutan en modo Release con optimizaciones completas
- Los tiempos incluyen overhead de GC y asignación de memoria
- Los resultados pueden variar según el hardware y la carga del sistema

### Ejecutar Benchmarks

```bash
# Ejecutar todos los benchmarks
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release

# Ejecutar benchmarks específicos
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release -- --filter "*Small*"
```

Los resultados completos se exportan automáticamente a `BenchmarkDotNet.Artifacts/results/` en formato Markdown, HTML y CSV.

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

El directorio `examples/spec_v2/` contiene todo el material del repositorio oficial [`toon-format/spec`](https://github.com/toon-format/spec/tree/main/examples):

- `conversions/` – pares JSON ↔ TOON publicados por la especificación.
- `valid/` – todos los ejemplos canónicos (key folding, delimitadores personalizados, arreglos primitivos, etc.).
- `invalid/` – casos límite que deben fallar en modo estricto.
- `basic_object`, `tabular_array`, `mixed_structures` – ejemplos propios de ToonSharp pensados para documentación rápida.

El conjunto de pruebas `ExamplesComplianceTests` recorre **cada** archivo TOON oficial y verifica que:

1. Los ejemplos válidos se puedan parsear, validar y hacer round-trip sin pérdida.
2. Los pares JSON ↔ TOON sigan equivalentes tras serializar/deserializar con ToonSharp.
3. Los ejemplos inválidos disparen `ToonSyntaxError` en modo estricto.

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

