---
applyTo: "src/Razor/**/*.{cs,vb}"
---

# Razor Tooling and Compiler Instructions for AI Coding Agents

These instructions complement the repository-wide `copilot-instructions.md` and apply to
all Razor sources under `src/Razor/`. Razor was merged into the Roslyn repo from
`dotnet/razor`, and most files keep their original sub-tree layout
(`src/Razor/src/Razor/...`, `src/Razor/src/Compiler/...`, `src/Razor/src/Shared/...`,
`src/Razor/src/Analyzers/...`).

## Critical Rules

- **Build wrappers**: Be careful passing `-projects` through `build.cmd` or PowerShell wrappers.
  Do not pass a semicolon-delimited project list through a nested PowerShell command invocation,
  because PowerShell can treat `;` as a statement separator and open `.csproj` files in
  Visual Studio. Prefer a single project at a time, or invoke the underlying script in a way
  that preserves the full `-projects` value as one argument.
- **Bug fixes**: Look for existing code that already handles the scenario before adding new code.
  The bug is more likely in existing logic than a missing feature.
- **Helpers**: Review existing helpers (`UsingDirectiveHelper`, `AddUsingsHelper`, etc.)
  before writing new utility methods. Don't duplicate.
- **Avoid blind `dotnet test` over the whole Razor tree**: Razor includes Playwright-based
  VS Code integration tests under
  `src\Razor\src\Razor\test\Microsoft.VisualStudioCode.Razor.IntegrationTests`. They require
  VS Code and waste significant time. Target a specific test project with
  `dotnet test path\to\Project.csproj` instead.

## File Types

- `.razor` -- Blazor components.
- `.cshtml` -- Razor views/pages (referred to as "Legacy" in the codebase).

## Code Patterns

- **Collections**: Use `ListPool<T>.GetPooledObject(out var list)` and `PooledArrayBuilder<T>`
  instead of allocating new collections. Prefer immutable collection types.
- **Positions**: Use `GetRequiredAbsoluteIndex` for converting positions to absolute indexes.
- **LSP conversions**: `sourceText.GetTextChange(textEdit)` converts LSP `TextEdit` to
  Roslyn `TextChange`. Reverse: `sourceText.GetTextEdit(change)`. Both live in
  `src\Razor\src\Razor\src\Microsoft.CodeAnalysis.Razor.Workspaces\Extensions\LspExtensions_SourceText.cs`.
- **RazorCodeDocument**: Immutable -- every `With*` method creates a new instance passing ALL
  fields through the constructor. When adding a new field, thread it through every existing
  `With*` method. Prefer computing derived data via extension methods (e.g.,
  `GetUnusedDirectives()`) rather than storing computed results as fields.
- **Razor documents in Roslyn**: Stored as additional documents. Resolve via
  `solution.GetDocumentIdsWithFilePath(filePath)` then `solution.GetAdditionalDocument(documentId)`.
- **Remote services**: Place the public stub method (calling `RunServiceAsync`) directly
  above its private implementation method.

## Testing

- Place `[WorkItem("url")]` on tests that track a specific issue (GitHub or DevOps URL).
- Use `TestCode` with `[|...|]` span markers for before/after test scenarios. Access
  `input.Text` (cleaned) and `input.Span` (marked range).
- Prefer raw string literals (`"""..."""`) over verbatim strings (`@"..."`).
- Test end-user scenarios, not implementation details.
- Verify/helper methods go at the bottom of test files. New test methods go above them.
- New tooling tests go in
  `src\Razor\src\Razor\test\Microsoft.VisualStudioCode.RazorExtension.UnitTests`
  (Cohosting architecture).
- Integration tests using `AdditionalSyntaxTrees` for tag helper discovery must set
  `UseTwoPhaseCompilation => true` (see `ComponentDiscoveryIntegrationTest`).

## Adding OOP Remote Services

When adding a new `IRemote*Service` and `Remote*Service`:

1. Interface: `src\Razor\src\Razor\src\Microsoft.CodeAnalysis.Razor.Workspaces\Remote\`
2. Implementation: `src\Razor\src\Razor\src\Microsoft.CodeAnalysis.Remote.Razor\`
3. Register in
   `src\Razor\src\Razor\src\Microsoft.CodeAnalysis.Razor.Workspaces\Remote\RazorServices.cs`
   (add to `MessagePackServices` or `JsonServices`).
4. **Add an entry to `eng\targets\RazorServices.props`** (at the Roslyn repo root, not under
   `src\Razor`):
   `Include="Microsoft.VisualStudio.Razor.{ShortName}"` with
   `ClassName="{FullTypeName}+Factory"`. The `ShortName` is your interface name with
   `IRemote` and `Service` stripped (e.g., `IRemoteFrobulatorService` becomes `Frobulator`).
5. Validate: `dotnet test src\Razor\src\Razor\test\Microsoft.CodeAnalysis.Remote.Razor.UnitTests --filter "FullyQualifiedName~RazorServicesTest"`

## VS Code Validation

Run Playwright E2E tests (the one place where `dotnet test` over a whole project tree is fine):

```powershell
cd src\Razor\src\Razor\test\Microsoft.VisualStudioCode.Razor.IntegrationTests
dotnet test
```
