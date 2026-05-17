# ToonSharp - TOON SPEC v2 (legacy README)

> **Default documentation:** the repository root [README.md](README.md) describes **TOON SPEC v3.0** (current default). Use this file for v2-oriented behavior notes or historical benchmark context from the v2.0.0 release line.

A production-grade C# library and CLI for JSON, YAML, TOML, and [TOON](https://github.com/toon-format/spec) conversion, aligned with **TOON SPEC v2.x** semantics and the official examples under `examples/spec_v2/`.

## v2 vs v3 (summary)

| Topic | v2 (this doc) | v3 ([README.md](README.md)) |
|-------|----------------|-----------------------------|
| List items with tabular first field | Often indented under `-` | **Section 10 canonical:** `- key[N]{fields}:` on the hyphen line |
| Array headers | Mixed | **`key[N]:`** length headers everywhere applicable |
| Tabular commas in cell values | Often switched to pipe delimiter | **Comma + quoted cells** per section 11 |
| Strings with spaces | Often quoted | **Unquoted when safe** per section 7.2 |
| Official conformance tests | `examples/spec_v2/` only | `tests/fixtures/spec/` **+** `examples/spec_v2/` |
| Typical test count | Lower | **528** tests (358 official fixtures + 66 examples + unit) |

## Spec compliance (v2 line)

- **Examples compliance:** `ExamplesComplianceTests` walks every file in `examples/spec_v2/` (valid, invalid, conversions).
- **Encode/decode** aligned with the published v2 example corpus and ABNF-oriented parser behavior.
- **Strict vs permissive** decode modes; validation API for tooling.
- **Key folding** (`safe`), custom delimiters (`,`, `|`, tab), and tabular auto-detection.

For the full v3 conformance matrix (official `tests/fixtures/spec/`, section 10, delimiter quoting rules, parser edge cases), see [README.md](README.md).

## Performance (v2.0.0 reference)

Benchmarks on **.NET 9**, BenchmarkDotNet, compared to **v1.4.2** (`main` baseline). The current default branch adds further optimizations (see root README).

| Operation | Workload | Mean (v2.0.0 era) | vs v1.4.2 |
|-----------|----------|-------------------|-----------|
| Serialize | Large table (200 rows) | ~468 us | ~-24% |
| Deserialize | Large table | ~373 us | ~-23% |
| Serialize | Large array (1000 inline items) | ~650 us | varies |
| Deserialize | Large array | ~541 us | ~-6% |
| Round-trip | Large array | ~1,239 us | ~+15% |
| Section 10 list-item | Serialize / deserialize / RT | ~5.8 / ~17.9 / ~25.9 us | - |

```bash
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release
.\scripts\compare-vs-main-baseline.ps1
```

## Installation and usage

Same package and API as the current release - see [README.md](README.md).

```bash
dotnet add package ToonLib
```

## Testing (v2 examples)

```bash
dotnet test -c Release --filter ExamplesComplianceTests
```

## License

MIT - see [LICENSE](LICENSE).