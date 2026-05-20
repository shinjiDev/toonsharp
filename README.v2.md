# ToonSharp - TOON SPEC v2 examples (companion README)

> **Default documentation:** [README.md](README.md) describes **ToonLib 2.0.0** (NuGet package), which implements **TOON SPEC v3.0**.
>
> **This file** documents the older **TOON SPEC v2** published example set under `examples/spec_v2/`. It is **not** documentation for ToonLib 1.4.x — upgrade to **2.0.0** for current library behavior.

## Library vs spec versions

| Name | What it means |
|------|----------------|
| **ToonLib 2.0.0** | Current NuGet / library release (breaking vs 1.4.x) |
| **TOON SPEC v3.0** | Format rules implemented by ToonLib 2.0.0 |
| **TOON SPEC v2 examples** | Files in `examples/spec_v2/` (this README) |
| **ToonLib 1.4.x** | Previous package line; pin if you need legacy output |

## v2 examples vs v3 behavior (summary)

| Topic | SPEC v2 examples (this doc) | ToonLib 2.0.0 / SPEC v3 |
|-------|------------------------------|-------------------------|
| List items with tabular first field | Often indented under `-` | **Section 10:** `- key[N]{fields}:` on hyphen line |
| Array headers | Mixed in examples | **`key[N]:`** length headers |
| Tabular commas in cells | Often pipe delimiter in old tooling | Comma + **quoted** cells per section 11 |
| Strings with spaces | Often quoted in old output | Unquoted when safe (section 7.2) |
| Automated tests | `ExamplesComplianceTests` (66) | + **358** official `tests/fixtures/spec/` (528 total) |

## Spec compliance (v2 example corpus)

- **ExamplesComplianceTests** walks every file in `examples/spec_v2/` (valid, invalid, conversions).
- Encode/decode aligned with the published v2 example corpus.
- For full v3 conformance (official fixtures, section 10, delimiter quoting, parser edge cases), see [README.md](README.md).

## Performance snapshot (early 2.0.0 branch vs ToonLib 1.4.2)

Benchmarks on **.NET 10**, BenchmarkDotNet, `main` = 1.4.2 baseline. Current numbers are in [README.md](README.md).

| Operation | Workload | Early 2.0.0 branch | vs 1.4.2 |
|-----------|----------|-------------------|-----------|
| Serialize | Large table (200 rows) | ~468 us | ~-24% |
| Deserialize | Large table | ~373 us | ~-23% |
| Serialize | Large array (1000 items) | ~650 us | varies |
| Deserialize | Large array | ~541 us | ~-6% |
| Round-trip | Large array | ~1,239 us | ~+15% |

```bash
dotnet run --project benchmarks/ToonSharp.Benchmarks -c Release
.\scripts\compare-vs-main-baseline.ps1
```

## Installation

```bash
dotnet add package ToonLib --version 2.0.0
```

## Testing (v2 examples only)

```bash
dotnet test -c Release --filter ExamplesComplianceTests
```

## License

MIT - see [LICENSE](LICENSE).