---
coverage: Compiler-layer (src/{Compilers,Dependencies,ExpressionEvaluator,Tools}) known issues, quirks & workarounds
---

# Compiler — Known Issues

Layer-specific quirks for the compiler. Load when working under
`src/{Compilers,Dependencies,ExpressionEvaluator,Tools}`. Cross-cutting issues
(generated code, CI marker gating, environmental test failures) live in
`.github/memory/KNOWN_ISSUES.md`.

## Microsoft.RoslynTools symbols package

- **Affected area:** `src/Tools/dotnet-roslyn-tools/Tool/`
- **Description:** The .NET tool package includes native libgit2 runtime assets.
  Linux `.so` files in the legacy `*.symbols.nupkg` cause symbol-publication
  errors.
- **Workaround:** `Microsoft.RoslynTools.csproj` runs
  `StripNativeLibrariesFromSymbolsPackage` after `Pack`. Keep that target aligned
  with the tool TFM and LibGit2Sharp package layout when either changes.
