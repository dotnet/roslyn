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
- New tooling tests go in
  `src\Razor\src\Razor\test\Microsoft.VisualStudioCode.RazorExtension.UnitTests`
  (Cohosting architecture).
- Integration tests using `AdditionalSyntaxTrees` for tag helper discovery must
  set `UseTwoPhaseCompilation => true` (see `ComponentDiscoveryIntegrationTest`).
- Regenerate baseline-backed compiler tests with a targeted test filter and
  `/p:GenerateBaselines=true` on one CoreCLR target framework, then rerun the
  tests normally. Two-phase tests can produce `.decl.codegen.cs` and
  `.decl.mappings.txt` in addition to implementation and component
  `.builder.txt` baselines.
- After Razor compiler tests or their `TestFiles` change, run the complete
  affected test project. A successful build does not validate embedded
  baseline resources.
