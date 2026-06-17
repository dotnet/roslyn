# HtmlCompletionDataGenerator

A code generator tool that transforms the VS HTML Editor's schema files (`html.xsd`, `CommonHTMLTypes.xsd`, `aria.xsd`, `html.loc`) into compiled C# lookup tables for use by Razor HTML completions.

## Purpose

Razor HTML completions previously delegated to an external HTML language server via a full LSP round-trip. For the ~80-90% of cases that are simple element/attribute lookups, this tool generates static in-process data structures that can serve completions with zero network latency and zero allocations.

## Usage

```powershell
dotnet run --project HtmlCompletionDataGenerator.csproj -- <schemaFilesDir> <outputFile>
```

Example:
```powershell
dotnet run -- "Q:\src\webtools\src\Languages\html\Editor\Schemas\SchemaFiles" "..\..\src\Microsoft.CodeAnalysis.Razor.Workspaces\Completion\Html\Generated\HtmlCompletionData.g.cs"
```

## Input Files

All sourced from `src/Languages/html/Editor/Schemas/SchemaFiles/` in the VS HTML Editor repo:

| File | Content |
|------|---------|
| `html.xsd` | 132 HTML elements, attribute definitions, complex types |
| `CommonHTMLTypes.xsd` | Shared attribute groups (core, events, microdata), simple types with enumerations |
| `I18Languages.xsd` | Language codes used by `lang`/`hreflang` attributes |
| `aria.xsd` | 35 ARIA attributes (supplemental, applied to all elements) |
| `1033/html.loc` | Element descriptions and documentation URLs |

## Output

Multiple `.g.cs` files in the target directory:
- `HtmlCompletionData.g.cs` — Public entry point with `GetElement(name)`, `AllElements`, and `GlobalAttributes`
- `HtmlElementInfo.g.cs` / `HtmlAttributeInfo.g.cs` — Struct definitions
- `HtmlElements.All.g.cs` — All element instances
- `HtmlAttributes.Shared.g.cs` / `HtmlAttributes.Unique.g.cs` — Attribute instances
- `HtmlAttributeValueGroups.Shared.g.cs` / `HtmlAttributeValueGroups.Unique.g.cs` — Value enumerations
- `HtmlAttributeGroups.Shared.g.cs` / `HtmlAttributeGroups.Unique.g.cs` — Per-element attribute arrays
- `HtmlElementGroups.Shared.g.cs` / `HtmlElementGroups.Unique.g.cs` — Per-element child arrays

## When to Regenerate

Run this tool when the HTML Editor schema is updated (typically 2-3 times per year). The generated files are checked in — not generated at build time — so changes are visible in code review.

## What's Included vs Excluded

**Included:** HTML elements, element-specific attributes, global attributes (core/events/microdata/ARIA), enumeration values, element descriptions.

**Excluded:** SVG (complex, 841KB — falls back to the external HTML completion provider), jQuery Mobile (`data-*` attributes — deprecated framework), CSS completions, JavaScript completions, file path completions.
