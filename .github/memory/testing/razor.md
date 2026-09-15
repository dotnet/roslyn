---
coverage: Razor-layer (src/Razor) test base classes & authoring conventions
---

# Razor — Testing

Layer-specific test guidance for Razor tooling/compiler tests under `src/Razor`.

## Conventions

- Use `TestCode` with `[|...|]` span markers for before/after scenarios. Access
  `input.Text` (cleaned) and `input.Span` (the marked range).
- Prefer raw string literals (`"""..."""`) over verbatim strings (`@"..."`).
- Test end-user scenarios, not implementation details.
- Verify/helper methods go at the bottom of test files; new test methods go above
  them.
- Shared cohost endpoint tests go in
  `src\Razor\src\Razor\test\Microsoft.CodeAnalysis.Razor.CohostingShared.UnitTests`
  and must be listed in its `.projitems` file; both the Visual Studio and VS Code
  unit-test projects import that suite.
- Cohost tests that exercise C# formatting options should supply `.editorconfig`
  paths and contents through `additionalFiles`. `CohostTestBase` registers files
  named `.editorconfig` as analyzer-config documents, including nested configs,
  so endpoints resolve options through the production path.
- Integration tests using `AdditionalSyntaxTrees` for tag helper discovery must
  set `UseTwoPhaseCompilation => true` (see `ComponentDiscoveryIntegrationTest`).
  Under two-phase compilation the `AdditionalSyntaxTrees` are compiled into a
  temp assembly and added as a *reference*, so discovery sees those types as
  coming from a referenced assembly (not source).
- Regenerate baseline-backed compiler tests with a targeted test filter and
  `/p:GenerateBaselines=true` on one CoreCLR target framework, then rerun the
  tests normally. Two-phase tests can produce `.decl.codegen.cs` and
  `.decl.mappings.txt` in addition to implementation and component
  `.builder.txt` baselines.
- After Razor compiler tests or their `TestFiles` change, run the complete
  affected test project. A successful build does not validate embedded
  baseline resources.
  
## Baseline (codegen) tests

- `ComponentCodeGenerationTestBase` and similar baseline tests assert generated
  IR/C#/mappings against files under
  `src/Razor/src/Compiler/Microsoft.AspNetCore.Razor.Language/test/TestFiles/IntegrationTests/ComponentCodeGenerationTest/<TestName>/`.
- Regenerate baselines by building/running the test project with
  `/p:GenerateBaselines=true` on one CoreCLR target framework, e.g.
  `dotnet test src\Razor\src\Compiler\Microsoft.AspNetCore.Razor.Language\test\Microsoft.AspNetCore.Razor.Language.UnitTests.csproj --filter FullyQualifiedName~<Name> /p:GenerateBaselines=true`.
  Tests always "pass" while generating (they overwrite baselines); re-run without
  the flag to actually validate, and `git diff` the `TestFiles` to review changes.
  Two-phase tests can also produce `.decl.codegen.cs` / `.decl.mappings.txt`
  (the declaration half) alongside the implementation and component baselines.
