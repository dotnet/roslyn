# Proposal: Warning Levels for Razor

> **Status:** The infrastructure described here was implemented in [dotnet/razor#13016](https://github.com/dotnet/razor/pull/13016) and ships in Razor 18.7. The shipped feature is named **warning levels** (`RazorWarningLevel`) rather than warning waves. No new warnings have been classified at non-default levels yet — that work is layered on top of this infrastructure as warnings are introduced.

## Summary

This proposal introduces **warning levels** to the Razor compiler, a feature modeled after [C#'s warning waves](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves). Warning levels allow users to adopt new .NET versions and Razor language features without being forced to immediately address new warnings that could break their build.

## Motivation

### The Problem

Currently, upgrading to a newer version of .NET brings the corresponding Razor language version by default as the language version is tied to the TFM. When new warnings are introduced in a language version, users with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` face a difficult choice:

1. **Fix all new warnings immediately** - This can be time-consuming and block upgrades, especially for large codebases.
2. **Downgrade the language version** - This allows the upgrade but prevents users from using new language features.
3. **Disable specific warnings** - This requires identifying each new warning and suppressing them individually, which is error-prone and may hide legitimate issues.

None of these options are ideal. Users should be able to upgrade their TFM and use new language features without being blocked by new warnings.

### The Solution

Warning levels decouple the introduction of new warnings from the language version. Users can:

- Upgrade their TFM and get the new Razor language version
- Set the warning level lower to suppress new warnings
- Still use all new language features
- Address new warnings at their own pace

## Design

### Warning Level Numbering

Each warning is tagged with an integer **warning level**. The compiler reports a warning only when its level is less than or equal to the configured `RazorWarningLevel`.

| Warning level | Description                                                                                     |
|---------------|-------------------------------------------------------------------------------------------------|
| `0`           | **Always reported.** Level 0 warnings are not part of any wave and are emitted at every level.  |
| `N` (>= 1)    | Reported only when `RazorWarningLevel` is `>= N`. Conventionally `N` matches the Razor language version major in which the warning was introduced (e.g. level `11` for warnings added in Razor 11). |

**Default behaviour.** When `RazorWarningLevel` is not set, the compiler uses `RazorLanguageVersion.GetDefaultWarningLevel()`, which currently returns the language version's major number. So a project on Razor 11 implicitly gets `RazorWarningLevel = 11` and therefore sees every level <= 11.

**Existing warnings are unaffected.** All warnings that exist today were authored without specifying a level, so they default to level `0` and continue to be reported regardless of the configured `RazorWarningLevel`. No project sees a behavior change until new warnings are added at higher levels.

### Configuration

Users configure the warning level via an MSBuild property that is forwarded to the source generator as an `AnalyzerConfigOptions` global option (`build_property.RazorWarningLevel`):

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <!-- Razor language version is 11.0 by default -->

  <!-- Stay on level 10 to suppress warnings introduced at level 11 -->
  <!-- All level-0 warnings continue to be reported -->
  <RazorWarningLevel>10</RazorWarningLevel>
</PropertyGroup>
```

The value must be empty or a non-negative integer. An empty value is treated as "use the default". Any other value (negative, non-numeric, etc.) produces diagnostic **`RZ3601`**: *Invalid value '{0}' for RazorWarningLevel. Must be empty or a non-negative integer.*

### Behavior

1. **Default behaviour.** With no configuration, the warning level equals the language version's major number, so users see every warning appropriate for their language version.

2. **Lowered warning level.** When `RazorWarningLevel` is set lower than the default:
   - Warnings whose level is greater than the configured value are suppressed.
   - All language features remain available.
   - Level-0 warnings continue to be reported.

3. **Raised warning level.** Setting `RazorWarningLevel` higher than the language version's default is allowed; it opts in to warnings introduced in future language versions.

4. **Invalid values.** Any value that is not empty and not a non-negative integer produces `RZ3601` and the compiler falls back to the default level.

5. **Errors are not affected.** Warning levels only control warnings, not compiler errors.

### Warning Classification

Each Razor warning declares its level on `RazorDiagnosticDescriptor`. Diagnostics expose the level via `RazorDiagnostic.WarningLevel`, and warnings without an explicit level default to `0`:

```csharp
// Example: A warning introduced at level 11 (Razor 11.0).
public static readonly RazorDiagnosticDescriptor Component_SomeNewWarning = new(
    "RZ10001",
    () => "Some new warning message",
    RazorDiagnosticSeverity.Warning,
    warningLevel: 11);
```

### Plumbing

The warning level is threaded through the compiler:

- `RazorConfiguration` carries `RazorWarningLevel` (default `0`).
- `RazorProjectEngine` copies it into `RazorCodeGenerationOptions.RazorWarningLevel` (with `WithRazorWarningLevel(int)` to mutate).
- `CodeRenderingContext.GetDiagnostics()` is the central filter: it keeps a diagnostic when `diagnostic.WarningLevel <= options.RazorWarningLevel`. Errors are always kept.
- The source generator (`RazorSourceGenerator.RazorProviders`) reads `build_property.RazorWarningLevel` from `AnalyzerConfigOptions`, parses it, and emits `RZ3601` on invalid input.

## Example Scenarios

### Scenario 1: Smooth Upgrade Path

A team with `TreatWarningsAsErrors` enabled wants to upgrade from .NET 10 to .NET 11:

```xml
<!-- Before: .NET 10 -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>

<!-- After: .NET 11 with new warnings suppressed -->
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <RazorWarningLevel>10</RazorWarningLevel>
</PropertyGroup>
```

The team can now:

- Use .NET 11 runtime and libraries.
- Use Razor 11.0 language features.
- Address new warnings incrementally by raising or removing `RazorWarningLevel`.

### Scenario 2: Gradual Warning Adoption

A team wants to adopt warnings one level at a time:

```xml
<!-- Start at level 10 -->
<RazorWarningLevel>10</RazorWarningLevel>

<!-- After fixing level-11 warnings, move up -->
<RazorWarningLevel>11</RazorWarningLevel>

<!-- Eventually catch up to current -->
<RazorWarningLevel>12</RazorWarningLevel>
```

## Implementation Status

The infrastructure landed in [#13016](https://github.com/dotnet/razor/pull/13016):

- ✅ `RazorWarningLevel` MSBuild property plumbed through the source generator (`build_property.RazorWarningLevel`).
- ✅ `WarningLevel` added to `RazorDiagnosticDescriptor` / `RazorDiagnostic` (defaults to `0`).
- ✅ `RazorConfiguration`, `RazorCodeGenerationOptions`, and `RazorProjectEngine` all carry the level.
- ✅ `CodeRenderingContext.GetDiagnostics()` filters warnings by level.
- ✅ `RZ3601` reported for invalid `RazorWarningLevel` values.
- ✅ `RazorLanguageVersion.GetDefaultWarningLevel()` defines the default (currently `Major`).

Still to do (follow-up work):

- Author the first batch of non-zero-level warnings.
- IDE/tooling integration so live diagnostics also respect the configured level.
- User-facing documentation listing each warning and the level at which it was introduced.

## Open Questions

1. Should `#pragma warning` directives be honoured as an override (e.g. so a project on level 10 can still opt into a single level-11 warning)? Not implemented in #13016.
2. Should we expose a `Latest` / `Preview` sentinel for early adopters, or is "set the integer high enough" sufficient?
3. What is the minimum warning level we should accept? Today the parser only enforces `>= 0`.

## References

- [C# Warning Waves Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves)
- [dotnet/razor#13016 — Implementation PR](https://github.com/dotnet/razor/pull/13016)
