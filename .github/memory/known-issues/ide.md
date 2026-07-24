---
coverage: IDE-layer (src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}) known issues, quirks & workarounds
---

# IDE — Known Issues

Layer-specific quirks for the IDE/Workspaces stack. Load when working under
`src/{Analyzers,CodeStyle,Features,Workspaces,EditorFeatures,VisualStudio,LanguageServer}`.
Cross-cutting issues live in `.github/memory/KNOWN_ISSUES.md`.

## Remaining Windows-only tests (issue #83159)

**Affected area:** `src/Features/CSharpTest/ConvertToRawString/` and `src/Analyzers/CSharp/Tests/MakeFieldReadonly/`
**Description:** 11 tests remain `WindowsOnly` after the elastic-newline fix. They cannot be made cross-platform because their test data contains literal `\r\n` content:
- 10 tests in `ConvertRegularStringToRawStringTests.cs` and `ConvertInterpolatedStringToRawStringTests.cs` — these convert string literals containing `\r\n` escape sequences to raw strings; the conversion preserves CRLF semantically, but the test's raw string expectations use the file's native LF on Linux, creating a mismatch.
- 1 theory in `MakeFieldReadonlyTests.cs` — `MultipleFieldsAssignedInline_LeadingCommentAndWhitespace` uses `InlineData("\r\n")` and `InlineData("\r\n\r\n")` C# escape sequences; these are always CRLF regardless of file line endings, but the test compares against raw strings that have LF on Linux.

**Workaround:** These tests are intentionally `[ConditionalFact/Theory(typeof(WindowsOnly), ...)]`.

## MEF composition failures surface as test failures

**Affected area:** MEF-dependent IDE/Workspaces tests
**Description:** A missing/incorrect MEF export attribute often manifests as an
unrelated-looking test failure rather than a clear composition error.
**Workaround:** When IDE tests fail unexpectedly, check the export attributes
first (`[ExportLanguageService]`/`[ExportWorkspaceService]`, `[Shared]`,
`[ImportingConstructor]` + `[Obsolete(MefConstruction.ImportingConstructorMessage)]`).
