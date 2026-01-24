# Proposal: Warning Waves for Razor

## Summary

This proposal introduces **Warning Waves** to the Razor compiler, a feature modeled after [C#'s warning waves](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves). Warning waves allow users to adopt new .NET versions and Razor language features without being forced to immediately address new warnings that could break their build.

## Motivation

### The Problem

Currently, upgrading to a newer version of .NET brings the corresponding Razor language version by default as the language version is tied to the TFM. When new warnings are introduced in a language version, users with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` face a difficult choice:

1. **Fix all new warnings immediately** - This can be time-consuming and block upgrades, especially for large codebases.
2. **Downgrade the language version** - This allows the upgrade but prevents users from using new language features.
3. **Disable specific warnings** - This requires identifying each new warning and suppressing them individually, which is error-prone and may hide legitimate issues.

None of these options are ideal. Users should be able to upgrade their TFM and use new language features without being blocked by new warnings.

### The Solution

Warning waves decouple the introduction of new warnings from the language version. Users can:

- Upgrade their TFM and get the new Razor language version
- Set the warning wave to a lower version to suppress new warnings
- Still use all new language features
- Address new warnings at their own pace

## Design

### Warning Wave Versioning

Warning waves will be versioned to match Razor language versions:

| Language Version | Warning Wave | Description                                        |
|------------------|--------------|----------------------------------------------------|
| 10.0             | 10.0         | **Baseline** - All existing warnings as of .NET 10 |
| 11.0             | 11.0         | New warnings introduced in .NET 11                 |
| 12.0             | 12.0         | New warnings introduced in .NET 12                 |
| ...              | ...          | ...                                                |

**Wave 10.0 is the baseline wave.** It represents all Razor warnings that exist today, before the introduction of warning waves. All current warnings will be classified as wave 10.0, meaning there is no behavior change for existing projects. New warnings introduced in future versions will be assigned to their corresponding wave (11.0, 12.0, etc.).

**By default, the warning wave matches the language version.** For example, if a project uses Razor language version 11.0, the warning wave defaults to 11.0.

### Configuration

Users can configure the warning wave independently of the language version:

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <!-- Language version is 11.0 by default -->

  <!-- Use wave 10.0 to suppress warnings introduced in wave 11.0 -->
  <!-- All existing warnings from wave 10.0 are still reported -->
  <RazorWarningWave>10.0</RazorWarningWave>
</PropertyGroup>
```

### Behavior

1. **Default behavior**: Warning wave equals language version. Users get all warnings appropriate for their language version.

2. **Lowered warning wave**: When `RazorWarningWave` is set lower than the language version:
   - Warnings introduced in waves higher than the configured wave are suppressed
   - All language features remain available
   - Existing warnings (from lower waves) continue to be reported

3. **Invalid configurations**:
   - Setting warning wave higher than language version should produce a diagnostic
   - Setting warning wave to an unrecognized version should produce a diagnostic

4. **Errors are not affected**: Warning waves only control warnings, not compiler errors.

5. **`#pragma warning` directives take precedence**: `#pragma warning disable/restore` directives take precedence over warning wave settings. This allows users to selectively enable a specific warning from a higher wave even when using a lower warning wave setting.

### Warning Classification

Each Razor diagnostic that represents a warning must be classified with the wave in which it was introduced:

```csharp
// Example: A warning introduced in wave 11.0
public static readonly RazorDiagnosticDescriptor Component_SomeNewWarning = new(
    "RZ10001",
    "Some new warning message",
    RazorDiagnosticSeverity.Warning,
    warningWave: RazorWarningWave.Wave11_0);
```

### Implementation Considerations

1. **Diagnostic Filtering**: The compiler must filter diagnostics based on the configured warning wave before reporting them.

2. **Backwards Compatibility**: All existing warnings will be classified as wave 10.0 (the baseline). This ensures no behavior change for existing projects—users will see the same warnings they see today. Only warnings introduced in future versions (11.0+) will be placed in higher waves.

3. **Documentation**: Each warning wave should be documented with the list of warnings it introduces.

## Example Scenarios

### Scenario 1: Smooth Upgrade Path

A team with `TreatWarningsAsErrors` enabled wants to upgrade from .NET 10 to .NET 11:

```xml
<!-- Before: .NET 10 -->
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>

<!-- After: .NET 11 with suppressed new warnings -->
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
  <RazorWarningWave>10.0</RazorWarningWave>
</PropertyGroup>
```

The team can now:

- Use .NET 11 runtime and libraries
- Use Razor 11.0 language features
- Address new warnings incrementally by eventually removing or updating `RazorWarningWave`

### Scenario 2: Gradual Warning Adoption

A team wants to adopt warnings one wave at a time:

```xml
<!-- Start with wave 10.0 -->
<RazorWarningWave>10.0</RazorWarningWave>

<!-- After fixing wave 10.0 warnings, move to 11.0 -->
<RazorWarningWave>11.0</RazorWarningWave>

<!-- Eventually catch up to current -->
<RazorWarningWave>12.0</RazorWarningWave>
```

## Implementation Plan

1. **MSBuild Integration**: Add `RazorWarningWave` property to the Razor SDK targets.
2. **Compiler Changes**: Add `WarningWave` property to `RazorDiagnosticDescriptor` to classify each warning.
3. **Diagnostic Filtering**: Update the compiler to filter diagnostics based on the configured warning wave.
4. **Tooling Updates**: Ensure IDE tooling (Visual Studio, VS Code) respects warning wave settings.
5. **Documentation**: Document all warnings and their associated waves.

## Open Questions

1. Should there be a way to opt-in to future warnings (set wave higher than language version) for early adopters?

2. What is the minimum warning wave version we should support? Are values like 8 valid, or only 10+?

3. Should we have a 'Latest' or 'Preview' warning wave value? Seems like if we want to do 1. then we would need these, but not otherwise the omission of the value serves the same purpose.

## References

- [C# Warning Waves Documentation](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/warning-waves)
