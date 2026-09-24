# Tilde Path Expansion for Blazor Components

## Summary

Tilde path expansion is a Razor compiler feature that transforms `~/`-prefixed
string literal attribute values in `.razor` files into `Assets[@"path"]` C#
expressions. This gives Blazor components a terse syntax for referencing
fingerprinted static web assets, consistent with MVC's `~/` convention.

```razor
@* Before: verbose *@
<img src="@(Assets["images/logo.png"])" />

@* After: tilde expansion *@
<img src="~/images/logo.png" />
```

Both produce the same generated C#:

```csharp
__builder.AddAttribute(1, "src", Assets[@"images/logo.png"]);
```

Expansion is opt-in: it applies only where the runtime has declared that an
attribute accepts asset paths (via `[AcceptsAssetPath]` for HTML element
attributes, or `[AssetPath]` on a component parameter). See **Scope** below.

## Motivation

.NET 9 added `ComponentBase.Assets` -- a `ResourceAssetCollection` indexer that
maps human-readable asset paths to fingerprinted URLs (e.g.,
`Assets["app.css"]` returns `"/app.fingerprint.css"`). Using it directly in
markup is verbose, requiring `@(Assets["..."])` with nested quotes and
parentheses.

MVC/Razor Pages have `UrlResolutionTagHelper`, which transforms `~/path` at
**runtime** using tag helper infrastructure. Blazor components don't support tag
helpers, so an equivalent mechanism must be a **compile-time** transformation in
the Razor compiler.

### Related specs and issues

| Source | Link | Notes |
|--------|------|-------|
| aspnetcore#68229 | https://github.com/dotnet/aspnetcore/issues/68229 | **Canonical proposal** -- authoritative API names, HTML allowlist, and cross-repo plan |
| aspnetcore#56076 | https://github.com/dotnet/aspnetcore/issues/56076 | Runtime APIs that introduced `Assets` indexer |
| AzDO#2623010 | DevDiv Razor Experiences board | Internal tracking work item |
| Razor-Language-Design#10 | Private | Language design discussion |
| aspnet/specs#769 | Private | Javier's design note |

## Detailed Design

### Syntax

The tilde-slash (`~/`) sequence at the start of a string literal attribute value
triggers expansion:

| Input | Generated C# | Notes |
|-------|-------------|-------|
| `src="~/images/logo.png"` | `Assets[@"images/logo.png"]` | Standard form |
| `src="~images/logo.png"` | `"~images/logo.png"` (literal) | No slash, no transformation |
| `src="~/~images.png"` | `Assets[@"~images.png"]` | Only leading `~/` is special |
| `src="normal.png"` | `"normal.png"` | No tilde, no transformation |
| `src="~/"` | `"~/"` (literal) | Empty path, no transformation |
| `src="@("~/img.png")"` | `"~/img.png"` (literal) | Explicit expression escapes `~/` |

### Scope

Expansion is **opt-in**. `~/` is only expanded where the target has explicitly
declared that it accepts asset paths; everywhere else `~/` is left as a literal.
This keeps existing markup backward-compatible (a `title="~/Hello"` is never
touched).

- **Component documents only** (`.razor` files). MVC `.cshtml` files already
  have `UrlResolutionTagHelper` and are not affected.
- **HTML element attributes** are expanded only when the `(element, attribute)`
  pair is declared via `[AcceptsAssetPath(element, attribute)]` (see below).
  For example, `<img src="~/logo.png">` expands only because the runtime
  declares `[AcceptsAssetPath("img", "src")]`.
- **Component parameter attributes** are expanded only when the target parameter
  property is annotated with `[AssetPath]`. For example, `<Image Source="~/x" />`
  expands only when `Image.Source` is marked `[AssetPath]`.
- **Pure string literals only**. Mixed content (e.g., `src="~/@(expr)/img.png"`)
  on an opted-in attribute is not expanded and produces diagnostic **RZ10029**.
- **Expression values** (`src="@myPath"`) are not affected -- the compiler
  cannot know at compile time whether `myPath` starts with `~`.

#### Opt-in attributes (defined by the runtime)

Both attributes live in `Microsoft.AspNetCore.Components` and are read by the
compiler purely by well-known metadata name; the compiler does not define them.

- **`[AssetPath]`** -- applied to a component parameter property (valid only on a
  `string`/`string?` `[Parameter]`). Marks that the parameter accepts `~/`
  asset-path expansion.
- **`[AcceptsAssetPath(string elementName, string attributeName)]`** -- applied
  (allowing multiples) to a public convention type named `AssetPathAttributes`
  (in `Microsoft.AspNetCore.Components.Web` for the built-ins). Each instance
  declares one HTML element/attribute pair that accepts expansion. This follows
  the same convention-class model as `BindAttributes` and `EventHandlers`: the
  compiler discovers the type by name during tag-helper discovery and reads the
  list. The built-in allowlist is `img[src]`, `link[href]`, and `script[src]`.

### Differences from MVC

| | MVC `UrlResolutionTagHelper` | Blazor tilde expansion |
|---|---|---|
| Timing | Runtime | Compile-time |
| Sigil | `~/` (slash required) | `~/` (slash required) |
| Scope | Hardcoded allowlist of URL attributes | Runtime-declared `[AcceptsAssetPath]` / `[AssetPath]` opt-in |
| Semantics | Prepends app base path | Looks up fingerprinted URL via `Assets` |
| Escape | N/A | Use `@("~/...")` explicit expression |

### Escaping

There is no dedicated escape syntax (e.g., `~~/`). Instead, users opt out of
expansion using Razor's existing explicit expression mechanism:

```razor
@* This will NOT be expanded -- it's a C# expression, not an HTML literal *@
<img src="@("~/images/logo.png")" />
```

The pass only operates on `HtmlAttributeValueIntermediateNode` and
`HtmlContentIntermediateNode` tokens (plain HTML string literals). An explicit
`@(...)` expression produces a `CSharpExpressionAttributeValueIntermediateNode`,
which the pass never visits. This means escaping works for free -- no special
compiler support needed.

**Rationale**: Introducing a dedicated escape sequence (like `~~/`) would add
permanent language surface area for a niche scenario. Razor already has a
universal escape mechanism (`@(...)`) that developers know. Reusing it avoids
inventing new syntax and keeps the feature's language footprint minimal.

### Language version gating

The feature is gated on **Razor language version 11.0**. Projects targeting
older language versions are unaffected -- the pass is a no-op. The `Assets`
property was introduced on `ComponentBase` in .NET 9.

### Diagnostic: RZ10029

**RZ10029**: "The '~' path prefix in '{0}' cannot be expanded because the
attribute value contains mixed content. Use '@(Assets[\"...\"])' for dynamic
paths."

Severity: Warning

Emitted when an attribute value contains a `~`-prefixed literal segment
alongside C# expressions or other literal segments (mixed content), on an
attribute that has opted into asset-path expansion. Applies to both HTML
element attributes and component parameter attributes.

### Diagnostic: RZ10030

**RZ10030**: "[AssetPath] on parameter '{0}' has no effect because its type is
not 'string'; '~/' asset-path expansion applies only to string parameters."

Severity: Warning

Emitted when `[AssetPath]` is applied to a component parameter whose type is not
`string` (or nullable `string`). The opt-in is ignored for such a parameter --
expanding `~/` would yield an `Assets[...]` string that isn't assignable to the
parameter -- so the attribute is flagged as having no effect.

## Implementation

### Compiler pass: `ComponentTildePathPass`

**Location**: `src/Razor/src/Compiler/Microsoft.CodeAnalysis.Razor.Compiler/src/Language/Components/ComponentTildePathPass.cs`

The pass is an `IRazorOptimizationPass` that runs during the
`DefaultRazorOptimizationPhase` at **Order = 75** -- after
`ComponentLoweringPass` (Order=0) creates the component/HTML attribute IR nodes,
but before `ComponentBindLoweringPass` (Order=100) rewrites bound attributes.

#### Pipeline position

```
ComponentLoweringPass (Order=0)
  |
  v  -- attribute nodes exist as HtmlAttributeIntermediateNode
  |     and ComponentAttributeIntermediateNode
  |
ComponentTildePathPass (Order=75)    <-- this pass
  |
  v  -- ~-prefixed values replaced with CSharp expression nodes
  |
ComponentBindLoweringPass (Order=100)
  |
  v
  ... (later passes)
```

#### IR tree transformations

**HTML element attributes** (`<img src="~/path" />`):

Before:
```
HtmlAttributeIntermediateNode
  -> HtmlAttributeValueIntermediateNode
       -> IntermediateToken (Content = "~/path")
```

After:
```
HtmlAttributeIntermediateNode
  -> CSharpExpressionAttributeValueIntermediateNode
       -> CSharpIntermediateToken (Content = "Assets[\"path\"]")
```

**Component parameter attributes** (`<Image Source="~/path" />`):

Before:
```
ComponentAttributeIntermediateNode
  -> HtmlContentIntermediateNode
       -> IntermediateToken (Content = "~/path")
```

After:
```
ComponentAttributeIntermediateNode
  -> CSharpExpressionIntermediateNode
       -> CSharpIntermediateToken (Content = "Assets[\"path\"]")
```

### Supporting changes

| File | Change |
|------|--------|
| `ComponentsApi.cs` | `Assets` constant on `ComponentBase`; metadata names for `AssetPathAttribute` and `AcceptsAssetPathAttribute` (plus the `AssetPathAttributes` candidate type name) |
| `PropertyMetadata.cs` | `AcceptsAssetPath` flag, set by `ComponentTagHelperProducer` when a `string` `[Parameter]` property also carries `[AssetPath]` (non-string parameters are flagged with RZ10030 instead) |
| `AssetPathMetadata.cs` | New descriptor-level metadata recording one `(element, attribute)` pair from `[AcceptsAssetPath]` |
| `AcceptsAssetPathTagHelperProducer[.Factory].cs` | New tag-helper producer (mirroring `EventHandlerTagHelperProducer`) that reads `[AcceptsAssetPath]` off the public `AssetPathAttributes` type; registered in `CompilerFeatures.cs` |
| `TagHelperKind.cs` / `TagHelperProducerKind.cs` / `MetadataObject.cs` | New `AssetPath` / `AcceptsAssetPath` enum members |
| `ComponentDiagnosticFactory.cs` | `TildePath_MixedContent` (RZ10029) and `AssetPath_NonStringParameter` (RZ10030) descriptors and factory methods |
| `RazorProjectEngine.cs` | Registered `ComponentTildePathPass(razorLanguageVersion)` in `AddComponentFeatures()` |

The pass builds a global element→attributes allowlist from the full set of
discovered tag helpers (via `ITagHelperFeature.GetTagHelpers()`), filtering for
`AssetPathMetadata` carriers. It reads the full set rather than the document's
in-scope tag helpers because the allowlist is compilation-global and must not be
subject to component namespace scoping.

### Test coverage

Tests live in `ComponentCodeGenerationTestBase` under the `#region Tilde Path Expansion` section. Each test produces baseline files in
`TestFiles/IntegrationTests/ComponentCodeGenerationTest/TildePath_*`. Opt-in
tests declare stub `[AssetPath]` / `[AcceptsAssetPath]` / `AssetPathAttributes` types via
`AdditionalSyntaxTrees` (they are not in the reference assemblies).

| Test | Scenario |
|------|----------|
| `TildePath_HtmlElement` | `<img src="~/images/logo.png">` with `(img, src)` opted in |
| `TildePath_HtmlElement_NotOptedIn` | Same markup, no `AssetPathAttributes` declaration -> not expanded |
| `TildePath_AttributeNotOptedIn` | `<img data-url="~/...">` -- only `src` opted in -> not expanded |
| `TildePath_ComponentParam` | `<Image Source="~/...">` with `[AssetPath]` on `Source` |
| `TildePath_ComponentParam_NotOptedIn` | Same markup, no `[AssetPath]` -> not expanded |
| `TildePath_AssetPathOnNonStringParameter_Warns` | `[AssetPath]` on a non-`string` parameter produces RZ10030 |
| `TildePath_WithSlash` | `<img src="~/css/app.css">` |
| `TildePath_ExplicitExpressionNotExpanded` | `@("~/...")` is not expanded (escape mechanism) |
| `TildePath_NoTilde` | No tilde, no transformation |
| `TildePath_MultipleElements` | `link/href` and `script/src` opted in |
| `TildePath_MultipleAttributesSameElement` | `<img srcset="~/a" src="~/b">` -- multiple opted-in attributes on one element both expand |
| `TildePath_MixedContent` | Mixed content on an opted-in attribute produces RZ10029 |
| `TildePath_NotExpandedBeforeLanguageVersion11` | Version gating |
| `TildePath_BackslashInPath` | Backslash escaping in generated C# |
| `TildePath_BareTildeNotExpanded` | Bare `~path` (no slash) is NOT expanded |
| `TildePath_EmptyPath` | Bare `~` left untouched |
| `TildePath_EmptyPathWithSlash` | `~/` left untouched |

## Design: opt-in expansion

Expansion is opt-in rather than always-on, preserving backward compatibility:
markup that predates the feature (or targets attributes the runtime has not
declared) is never rewritten. Two mechanisms cover the two attribute kinds.

### HTML element attributes -- `[AcceptsAssetPath]` allowlist

The runtime declares which element/attribute pairs accept asset paths by
applying `[AcceptsAssetPath(elementName, attributeName)]` (allowing multiples) to
a public convention type named `AssetPathAttributes`, e.g.:

```csharp
namespace Microsoft.AspNetCore.Components.Web;

[AcceptsAssetPath("img", "src")]
[AcceptsAssetPath("link", "href")]
[AcceptsAssetPath("script", "src")]
public static class AssetPathAttributes { }
```

`AcceptsAssetPathTagHelperProducer` discovers this type by name during tag-helper
discovery (exactly as `EventHandlerTagHelperProducer` discovers `EventHandlers`)
and emits one carrier `TagHelperDescriptor` per pair, carrying `AssetPathMetadata`.
`ComponentTildePathPass` collects those carriers into a case-insensitive
element→attributes map and expands `~/` on an HTML attribute only when its owning
element and name are in the map. The owning element name comes from the enclosing
`MarkupElementIntermediateNode.TagName`, tracked as the walker descends.

### Component parameter attributes -- `[AssetPath]` opt-in

Component parameters are arbitrary C# properties with no standard naming
convention, so an attribute-name allowlist is not feasible. Instead the parameter
property is annotated:

```csharp
public class Image : ComponentBase
{
    [Parameter]
    [AssetPath]  // enables ~/path expansion for this parameter
    public string Source { get; set; }
}
```

`ComponentTagHelperProducer` records this as `PropertyMetadata.AcceptsAssetPath`
on the bound attribute. The pass expands `~/` on a
`ComponentAttributeIntermediateNode` only when its `BoundAttribute` metadata has
`AcceptsAssetPath` set. Without it, `~/` in a component parameter value is left as
a plain string literal.

`[AssetPath]` is only honored on a `string` (or nullable `string`) parameter --
expanding `~/` yields an `Assets[...]` string, which wouldn't be assignable to a
parameter of any other type. When `[AssetPath]` is applied to a non-string
parameter the producer records no opt-in and instead attaches the **RZ10030**
warning to the bound attribute, so the misuse surfaces at compile time.

### Why not always-on

An always-on design (expand `~/` in every attribute, using the `~/` prefix as the
sole signal) is simpler but risks false positives on non-URL attributes
(`title="~/Hello"`, `<Tooltip Text="~/not-a-path" />`) and silently changes the
meaning of existing markup. The opt-in design costs a small amount of runtime API
surface (`[AssetPath]`, `[AcceptsAssetPath]`) in exchange for being
backward-compatible and explicit about which attributes participate.

### No MSBuild opt-out

The canonical proposal lists a default-on `RazorEnableAssetPathExpansion` MSBuild
property (surfaced as `RazorConfiguration.EnableAssetPathExpansion`) as a
per-project opt-out. This is intentionally **not** implemented: because expansion
is already opt-in at the point of use -- a value is only rewritten when its
parameter carries `[AssetPath]` or its element/attribute pair is declared via
`[AcceptsAssetPath]` -- there is no ambient behavior to switch off. A project that
wants no expansion simply references no opt-in declarations. Adding a global gate
would duplicate that control without covering any case the attributes don't
already cover.

