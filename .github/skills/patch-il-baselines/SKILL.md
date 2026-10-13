---
name: patch-il-baselines
description: Update expected IL baselines in compiler unit tests from Actual output for tests that are explicitly reported as failed. Use when Test Explorer or test output shows IL baseline differences and the requested fix is limited to those failed tests.
---

# Patch Failed IL Baselines

Use this skill when compiler unit tests fail because the emitted IL differs from the expected `VerifyIL` baseline.

## Scope rules

- Treat the current Test Explorer **failed-tests list** as the only source of truth for scope. Do not infer scope from names, prior failures, compiler output, or tests that happen to look similar.
- Before editing, record the exact fully qualified names listed as failed, including the target framework when Test Explorer distinguishes framework-specific failures.
- Patch only those recorded tests. Do not update passing, skipped, unrelated, or merely discovered tests, even when they have similar IL or share a source file.
- Work on one test at a time: select one recorded failure, patch only its baseline, run only that test, and finish or correct it before selecting the next failure.
- Keep each test's investigation and edit isolated. Never replace multiple baselines in one broad operation or use a class/prefix filter unless every test selected by that filter is in the recorded failed list.
- Preserve existing user changes and the original indentation/offset of each baseline.
- Do not change compiler behavior, test source, expected program output, diagnostics, or IL-verification messages unless the failure's Actual output specifically identifies that assertion as the failed baseline.

## Workflow

1. Read the relevant compiler testing guidance and identify the test project and source file.
2. Capture the failed test names from the current Test Explorer result and make a fixed checklist. Treat duplicate entries for different target frameworks as one source test, but retain all reported frameworks for validation.
3. Choose exactly one unchecked failed test from the checklist. Do not begin work on another test until this one passes or is explicitly blocked.
4. Run or inspect only that selected test and locate its failing `VerifyIL` or equivalent IL baseline assertion from the stack trace.
5. Copy the complete `Actual:` block into that assertion:
   - replace the entire expected IL body, including code size, locals, instructions, and closing brace;
   - preserve the surrounding string syntax and indentation;
   - for verbatim `@"..."` strings, double embedded quotes as required by C#;
   - for raw `"""..."""` strings, keep embedded quotes unescaped;
   - for a multiline expected-output or IL-verifier-message assertion, patch it only when that assertion is the one reported as failed.
6. Review the diff and confirm the selected test is the only baseline changed. Do not accept broad reindentation or unrelated changes.
7. Build the affected test project if needed, then run exactly the selected test across every target framework reported for it.
8. If the selected test still fails, use its new `Actual:` block and repeat only for that same test. Do not move to the next checklist item until it passes.
9. Mark the selected test complete, then return to step 3 for the next recorded failure. After all items pass, review the final diff and confirm no passing or skipped test was modified.

## Recommended commands

```powershell
# Run one test exactly as reported
dotnet test <path-to-test-project.csproj> --filter "FullyQualifiedName=<fully-qualified-test-name>"

# Run a captured set from one test class only when every selected test was reported failed
dotnet test <path-to-test-project.csproj> --filter "FullyQualifiedName~<captured-prefix>"
```

Use the Visual Studio Test Explorer runner when available so its Test Output contains the complete Expected/Actual comparison.

## Completion criteria

- Every test originally listed as failed passes after the baseline update.
- No passing or skipped test was modified.
- The affected test project builds successfully.
- The final diff contains only the requested baseline changes and preserves nearby formatting.
