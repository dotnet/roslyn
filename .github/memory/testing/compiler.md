---
coverage: Compiler-layer (src/{Compilers,Dependencies,ExpressionEvaluator,Tools}) test base classes & authoring conventions
---

# Compiler — Testing

Layer-specific test guidance for compiler tests under `src/Compilers/*/Test/`.

## Test structure

Inherit from language-specific base classes: `CSharpTestBase` for C#,
`VisualBasicTestBase` for VB.

```cs
public class MyTests : CSharpTestBase
{
    [Fact]
    public void TestMethod()
    {
        var comp = CreateCompilation(sourceCode);
        // Test compilation, symbols, diagnostics
    }
}
```

## Conventions

- **Unit tests** target individual compiler phases (lexing, parsing); **compilation
  tests** create `Compilation` objects and verify symbols/diagnostics.
- **Cross-language patterns**: many test patterns work for both C# and VB with
  minor syntax changes.
- **Verification baselines**: when helpers like `VerifyDiagnostics`,
  `VerifyEmitDiagnostics`, `VerifyIL`, and similar compiler test APIs fail with an
  `Actual:` block containing the expected content, copy that block directly into
  the verification call.
- **Use `comp.VerifyEmitDiagnostics()`** (rather than only `VerifyDiagnostics`) so
  reviewers can see whether the code under test is legal.
- **Keep tests focused**: do the minimal work to reach the core assertions; use
  `Single()` instead of checking counts then indexing.
- **Prefer raw string literals** (`"""..."""`) over verbatim strings (`@"..."`)
  for test source code.
- **Localized command-line diagnostics**: compare message arguments using the
  corresponding `CodeAnalysisResources` format string, not hard-coded English
  punctuation. Analyzer-config descriptors cache resource strings at their
  first use, which may happen in another test running under the host UI culture
  before a compiler with `/preferreduilang:en` runs. Account for both cultures
  when checking output from those diagnostics.
