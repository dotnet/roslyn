---
coverage: Measured fast build and test slices for iterative agent validation
---

# Agent Inner-Loop Validation

Use a supported slice while iterating on a focused change. A slice maps one
component to its product project, test-project graph, representative source edit,
and test filter. The measurement infrastructure is component-neutral and requires
explicit slice selection.

The initial pilots cover C# CodeStyle and formatting. Do not use a slice for
unrelated IDE, compiler, or Razor changes. Add and measure a component-specific
slice before documenting another area as supported.

The slices are not the final validation gate. Before declaring work complete, run
the affected project build and targeted tests required by the Definition of Done in
`.github/copilot-instructions.md`.

## Measuring a slice

Run the measurement script from the repository root:

```powershell
pwsh -NoProfile -File eng/measure-agent-inner-loop.ps1 -slice <slice-name> -iterations 3
```

The script restores and prepares the slice, touches a representative product
source file before each validation build, and runs filtered tests. It restores the
file's original timestamp afterward, prints median timings, and writes detailed
command, duration, environment, test-count, and exit-code data under
`artifacts/log/`. It fails when the filter executes zero tests.

Each slice defines a stable representative filter for benchmarking. For a real
change within the selected component, pass the affected test class:

```powershell
pwsh -NoProfile -File eng/measure-agent-inner-loop.ps1 `
  -slice CSharpFormatting `
  -testFilter "FullyQualifiedName~MyAffectedTests"
```

Use `-configuration Release` to measure Release builds and `-outputPath <path>`
to select the JSON output file.

Use `-skipPreparation` only when the product and test projects are already built.
Do not use it after changing dependencies, generated inputs, target frameworks, or
build configuration.

## Supported slices

| Slice | Use for | Product build | Test project and representative filter | Final-validation escalation |
|---|---|---|---|---|
| `CSharpCodeStyle` | C# IDE code-style analyzers and code fixes | `src/CodeStyle/CSharp/CodeFixes/Microsoft.CodeAnalysis.CSharp.CodeStyle.Fixes.csproj` | `src/CodeStyle/CSharp/Tests/Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests.csproj` (`$(NetRoslyn)`); `FullyQualifiedName~Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.AddRequiredParentheses.AddRequiredPatternParenthesesTests` | Run the test class or classes for the changed analyzer/fix, then the full C# CodeStyle test project when shared analyzer infrastructure changes. |
| `CSharpFormatting` | C# syntax formatting and formatting rules | `src/Workspaces/CSharp/Portable/Microsoft.CodeAnalysis.CSharp.Workspaces.csproj` | `src/Workspaces/CSharpTest/Microsoft.CodeAnalysis.CSharp.Workspaces.UnitTests.csproj` (`$(NetVSShared)`); `FullyQualifiedName=Microsoft.CodeAnalysis.CSharp.UnitTests.Formatting.FormattingTests.Format1` | Run the affected formatting test class; run the full C# Workspaces test project when shared formatting infrastructure changes. |

## Reference measurement

The slices were measured in Debug configuration with a warm NuGet package cache
on Windows, 32 logical processors, and .NET SDK
`11.0.100-preview.6.26359.118`.

| Slice | Restore | Initial product build | Test preparation | Representative-edit build median | Filtered test median |
|---|---:|---:|---:|---:|---:|
| `CSharpCodeStyle` (9 tests) | 6.5 seconds | 8.8 seconds | 74.3 seconds | 12.5 seconds | 9.1 seconds |
| `CSharpFormatting` (1 test) | 3.1 seconds | 5.0 seconds | 8.0 seconds | 10.4 seconds | 3.7 seconds |

Timings are reference values, not universal pass/fail thresholds. Re-run the script
on the current environment when evaluating whether a slice remains fast enough for
iterative use.
