---
applyTo: "src/Razor/**/*.{cs,vb}"
---

# Razor Tooling and Compiler Instructions for AI Coding Agents

Razor was merged into the Roslyn repo from `dotnet/razor`, and most files keep
their original sub-tree layout
(`src/Razor/src/Razor/...`, `src/Razor/src/Compiler/...`, `src/Razor/src/Shared/...`,
`src/Razor/src/Analyzers/...`).

## Critical Rules

- **Bug fixes**: Look for existing code that already handles the scenario before adding new code.
  The bug is more likely in existing logic than a missing feature.
- **Helpers**: Review existing helpers (`UsingDirectiveHelper`, `AddUsingsHelper`, etc.)
  before writing new utility methods. Don't duplicate.
- **Warning levels**: Track warnings with non-zero `RazorWarningLevel` values in
  [`docs/razor/warning-levels.md`](../../docs/razor/warning-levels.md), including the diagnostic
  ID, warning level, exact message, and trigger condition.

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
- **Razor documents with virtual URIs**: Remote Razor document classification preserves the full
  additional-document `FilePath` for identity. For parseable absolute URI file paths, inspect the
  URI's local path when checking the `.razor` or `.cshtml` extension; do not strip the query from
  the stored file path.
- **Remote services**: Place the public stub method (calling `RunServiceAsync`) directly
  above its private implementation method.
- **Formatting options across OOP**: Cohost endpoints must resolve
  `CSharpSyntaxFormattingOptions` from the Razor document's analyzer-config options with
  `CSharpFormattingOptionsHelper.GetCSharpSyntaxFormattingOptions(razorDocument, cancellationToken)`.
  This applies `.editorconfig` sections matching the `.razor` or `.cshtml` path and falls back to
  the user's global C# options. Include the resolved options in `RazorFormattingOptions` sent to
  remote formatting consumers; remote `IClientSettingsManager` state does not contain the user's
  C# formatting preferences.
- **Runtime-declared attribute lists**: When the runtime declares a set the compiler must read
  (e.g. `[EventHandler]`, `[AcceptsAssetPath]`), it applies the attributes to a public type with
  a well-known name (`EventHandlers`, `AssetPathAttributes`). A `TagHelperProducer` under
  `Language/TagHelpers/Producers/` keys off that type name (`IsCandidateType`) and emits carrier
  `TagHelperDescriptor`s whose descriptor-level metadata carries the parsed values. A later
  optimization pass reads the full discovered set via `ITagHelperFeature.GetTagHelpers()` (not the
  document's in-scope tag helpers, which are namespace-scoped) and filters by metadata kind.
- **Visual Studio options**: Register Razor Advanced settings in
  `Microsoft.VisualStudio.RazorExtension\UnifiedSettings\razor.registration.json`, localize
  their UI text in `VSPackage.resx`, read them through `OptionsStorage`, and add remotely consumed
  values to `ClientAdvancedSettings` so `IClientSettingsManager` synchronizes changes live.

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
