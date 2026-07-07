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
