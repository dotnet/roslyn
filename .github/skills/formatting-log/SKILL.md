---
name: formatting-log
description: Import Razor formatting log zips that the user has already downloaded from Azure DevOps feedback tickets or GitHub issues into FormattingLogTest, validate whether the captured problem still reproduces, and if needed drive a minimal repro plus fix workflow.
---

# Razor Formatting Log Investigation

Use this skill when:
- investigating a Razor formatting issue from Visual Studio or VS Code feedback
- given an Azure DevOps work item, Developer Community ticket, or GitHub issue that includes a formatting log zip
- asked to import a `Full_*_razor.zip` or `Range_*_razor.zip` archive into the test suite

## Goal

1. Get a local path to the formatting log archive.
2. Import every file from that zip into `src\Razor\src\Razor\test\Microsoft.VisualStudio.LanguageServices.Razor.UnitTests\TestFiles\FormattingLog\<TestName>\`.
3. Add a matching `[Fact]` to `FormattingLogTest.cs`.
4. Run the imported formatting log test.
5. If it passes, report that the issue captured in the logs no longer reproduces.
6. If it fails, keep the imported repro, reduce it to a minimal `DocumentFormattingTest`, and then fix the product code.

## Finding the archive

### Azure DevOps / VS feedback ticket

1. Read the work item and comments with the Azure DevOps tools so you know which attachment to ask for.
2. Do **not** try to download Developer Community or Azure DevOps feedback attachments yourself. The available tooling cannot reliably fetch those private files.
3. Ask the user to download the `Full_*_razor.zip` or `Range_*_razor.zip` attachment locally and give you the absolute zip path.
4. Continue only after the user has provided that local path.

### GitHub issue

1. Read the issue body and comments.
2. Look for uploaded zip attachments or linked archives with the same naming pattern.
3. If the archive is directly downloadable, download it to a temporary folder. Otherwise ask the user to download it and provide a local path.

## Import the logs

Use the helper script in this skill:

```powershell
.\.github\skills\formatting-log\scripts\Import-FormattingLog.ps1 `
  -ZipPath C:\Users\dawengie\Downloads\Range_Index_razor.zip `
  -WorkItemUrl https://dev.azure.com/devdiv/DevDiv/_workitems/edit/2878103 `
  -TestName RanOutOfOriginalLines
```

- `-ZipPath` must be a local file path. For Developer Community and Azure DevOps feedback tickets, ask the user to download the archive first instead of trying to fetch it yourself.
- `-TestName` defaults to a sanitized name derived from the zip filename. Override it when you want a shorter or more descriptive repro name.
- Keep the folder name and the test name identical.
- Use `-Expectation Null` only when the correct outcome is that all edits should be filtered out. Otherwise keep the default `NotNull`.
- The script copies every extracted file into the new test asset folder and appends the matching test method to `FormattingLogTest.cs`.

## Run the imported test

```powershell
dotnet test src\Razor\src\Razor\test\Microsoft.VisualStudio.LanguageServices.Razor.UnitTests\Microsoft.VisualStudio.LanguageServices.Razor.UnitTests.csproj --filter "FullyQualifiedName~FormattingLogTest.<TestName>"
```

Do **not** run `dotnet test` at the repo root.

## Interpreting the result

- **Test passes**: report that the problem captured in the logs no longer reproduces.
- **Test fails**: continue with a minimal repro and product fix.

## When the imported log test fails

1. Commit the imported formatting log assets and `FormattingLogTest` change by themselves.
2. Reduce the failure to a focused repro in `src\Razor\src\Razor\test\Microsoft.CodeAnalysis.Razor.CohostingShared.UnitTests\Formatting\DocumentFormattingTest.cs`.
3. Commit the minimal repro by itself.
4. Fix the product code.
5. Commit the fix by itself.
6. Re-run the imported `FormattingLogTest` plus the focused `DocumentFormattingTest` coverage.

The formatting-log test, the minimal repro, and the fix should stay as separate commits during the investigation.

## Notes

- If the user only gives you a Developer Community or Azure DevOps ticket URL, stop and ask them to download the zip before proceeding.
- `FormattingLogTest` accepts both real range payloads and literal `null` `Range.json` files from full-document logs.
- Imported log zips often contain more files than the test currently consumes. Keep them all in the test asset folder.
- Prefer adding `[WorkItem("...")]` with the originating Azure DevOps or GitHub URL on the imported test.
