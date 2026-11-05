## Summary
This folder contains Roslyn's copy of the generated LSP and VS protocol model, together with Roslyn-owned converters and server extensions. The generated snapshot uses the LSP 3.18 metamodel; importing it does not enable new server capabilities.

These types are shared via restricted IVTs to Razor and XAML, as they run inside the C# Roslyn LSP server.

## Generated snapshot and updates

`Generated` comes from `src\Protocol\LanguageServer.Protocol.Internal\Generated` in VSLanguageServerClient commit `f72be3306748a6c07a74366c1a774def4bb0fb19`, the producer of Protocol.Internal 18.14.8 and Client/Implementation 18.14.30. It contains 481 C# files. No producer handwritten converters, support implementation, or client serializer context are copied.

| Source | Immutable Git object |
|---|---|
| Generated tree | `fbe2def9691bedeae221c82586ceeb91f6b3ea04` |
| Generator tree (`test\tools\LspProtocolGenerator`) | `025b536a843b21b8f294790f663fb3a8df133b7b` |
| Generator `Program.cs` | `f9510b6dde9729b078e715233269391d28a83212` |
| Core `MetaModel\metaModel.json` (3.18.0) | `480e5e89a725db765df9e423d479c8ce1fe3781b` |
| VS `MetaModel\vs-metaModel.json` | `753bcf0170ac9a9c85b9c4849a8f80a8c694cc11` |
| Companion `MetaModel\metaModel.schema.json` | `6ff5181239cdd8b246cd0d5b86eb8b7a45582fa4` |

The producer records a mutable upstream metamodel URL, not an upstream revision. The pinned input blobs, rather than a fresh download, identify this snapshot. The companion JSON schema is not a loaded generation input.

To reapply the Roslyn adaptations, obtain the authorized, byte-exact generated tree from that commit and run from the Roslyn root:

```powershell
.\eng\update-lsp-protocol.ps1 -SourceDirectory <path-to-unmodified-Generated>
```

The script verifies the source Git tree before writing and reproduces all changes within `Generated`. Edit its transformations, not the generated files. Keep additive customizations in `Extensions` and behavior in the owned converters/support. The producer's generator deletes C# files recursively in its output directory: never run it against this mixed source folder. A different source snapshot requires a new provenance and redistribution-rights review; source availability or a package download alone does not establish copying rights.

### Roslyn deviations

| Area | Retained behavior / implementation |
|---|---|
| Ownership | `Roslyn.LanguageServer.Protocol`, internal top-level declarations, existing assembly/target frameworks. No Protocol.Internal or VS UI binary dependency. |
| URIs | `DocumentUri`, `ParsedUri`, and `DocumentUriConverter` retain original strings and defer vscode-uri-compatible parsing, including invalid input. Generated URI aliases use this wrapper, not the producer deferred wrapper. Workspace-edit map keys remain raw strings. Existing eager `System.Uri` contracts remain for notebook identifiers/documents, show-document requests, URI-presentation arrays, and related-document diagnostic map keys. |
| Member policies | Explicit member policies in the import script preserve baseline requiredness, null/default omission, nullable booleans, enum defaults, the `preferred` code-action key, and nullable reference-item locations. No global omit-default policy is installed. |
| Initialization / sync | Retained root fields, trace default, workspace-folder normalization, default text synchronization, and numeric-sync-to-options coercion. Save options retain the original shape. |
| Unions | The owned `UnionConverter` recognizes native `IUnion` reference wrappers, discovers single-argument constructors, and selects constant get-only string discriminators without `KindAttribute`. Token/array filtering, raw URI arms, nested unions, and hover ambiguity retain Roslyn behavior. Generated unions have a type-level converter attachment as well as options registration because Razor also uses editor-owned RPC options. Failed arm probes catch JSON conversion failures, not arbitrary application exceptions. Server-only contracts use named native unions; no legacy sum-type implementation remains. |
| Progress / dispatch | Typed server partial results and work-done reporting, `ITextDocumentParams`, and writable document-selector registration metadata remain available. Transport and request/formatter option separation are unchanged. |
| Server-only members | Retained notebook contracts, deprecated fields still used by Roslyn, related-document diagnostic reports/partial results, registration extensions, and Roslyn-specific initialization/hover/symbol payloads that the producer client subset omits. |
| Formatting / signatures | Formatting extras remain limited to bool, int32, and string. Generated signature-label integer arrays retain the old tuple-object wire shape. Missing/null labels remain rejected on write. |
| Completion | The owned optimized writer/cache and Razor completion subclass are rebased onto the merged generated types. Existing omission rules and item/list commit-character shapes are retained. |
| Object / rendering | The owned natural-object converter, imaging/adornment types, wire discriminators, and validated rendering setters remain in use. No producer client serializer context or AOT guarantee is introduced. |
| Value behavior | Owned position/range, diagnostic/location/symbol, document-identifier, icon, project-context, and kind/modifier behavior is retained in partials; DataTip and diagnostic-option records retain value semantics. Diagnostic equality/hash and caller comparisons use union payloads rather than wrapper identity. |
| Workspace symbols | The existing nullable scalar `tags` shape is retained rather than adopting the generated array. |

### CLR migration and partner coordination

This replacement deliberately adopts the generated CLR API and is **not binary compatible** with the previous model. In-repository Roslyn/Razor callers move together; separately built XAML and other IVT consumers must be coordinated and rebuilt before insertion/publication. Unchanged assembly identity and empty PublicAPI baselines do not imply binary compatibility.

Concrete breaks include merged VS/core identities (for example `VSInternalCompletionItem` becomes `CompletionItem`), capability/registration type names, `DocumentUri`/`RootDocumentUri` accessor names becoming `Uri`/`RootUri`, generated property names such as `Ch` and `Type`, request descriptor `Name` becoming `Method`, union-of-array versus array-of-union shapes, integer-array signature labels, and per-property generated union identities. Standard and VS capability collisions remain distinct: for example `DiagnosticProvider` versus `VsDiagnosticProvider`, and completion `CompletionList` versus `VsCompletionList`.

Generated unions are reference objects, not value-equal `SumType` structs. Use C# union patterns and compare arm payloads; do not rely on wrapper equality or legacy `First`/`Match` APIs. A null reference union serializes as JSON null, unlike an empty nonnullable legacy union value. Nullable endpoint results must remain nullable; this CLR distinction is not a new server capability.

Merged identities also remove subtype-specific equality overloads. In particular, document-identifier equality retains the core URI/version rules; it cannot simultaneously preserve the old VS subtype's project-context-sensitive object equality as a separate type contract. Its display string still includes project context.

## Breaking Changes
The examples below describe the former `SumType` model. This snapshot instead adopts generated unions and requires coordinated partner updates; it does not add the intermediate-property shims illustrated below.

Ensuring that these types are not binary breaking on changes is important, as this dll is shared between Roslyn, Razor, and XAML in both VSCode and VS.  They export handlers that are used in our Roslyn LSP server using protocol types.

In general, the LSP specification itself generally does not make JSON protocol breaking changes.  New additions are controlled by capabilities, properties are only added, etc.  Most of the time these kinds of changes are not binary breaking for our type definitions either - it's totally fine to add new properties, methods, etc. to our type definitions

However, some protocol changes can result in binary breaking changes.  The main scenario for this is when the protocol changes a property to a union or adds another definition to a union type.  For example, if initially the server capabilities type defined a `hoverProvider`:
```json
hoverProvider?: boolean;
```
In our C# type definitions this would be defined as
```csharp
[JsonPropertyName("hoverProvider")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public bool? HoverProvider
{
    get;
    set;
}
```

It is totally legal (and not breaking) in the LSP protocol to modify this type definition into a union type, controlling what is defined based on a capability.
```json
hoverProvider?: boolean | HoverOptions;
```
and in C# this would be defined as:
```csharp
[JsonPropertyName("hoverProvider")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public SumType<bool, HoverOptions>? HoverProvider
{
    get;
    set;
}
```

which is now a breaking change due to the property type changing from `bool?` to `SumType<bool, HoverOptions>?`.  And this same logic applies to adding a new value to a union type as going from `SumType<T, U>` to `SumType<T, U, V>` is also a breaking change.

### Handling breaking changes

Generally, changes that cause binary breaking changes are relatively rare (adding new types to a union).  Additionally, we only need to be careful about binary breaking changes if our partners actually use the API being changed.  If no one uses it, we can just update the property with a breaking change.

However, if a partner is using the type, we need to handle breaking changes to it carefully.  We can support this by adding a new intermediate property for the new union type definition, obsoleting the old one, switching our partners to the new property, then move everything back:

### 1.  Add new intermediate property.
First, we add a new property representing the union version of the type and move the serialization attributes to it.  The old property is then implemented by accessing the correct value of the new union type.  This is safe as the new `HoverOptions` is not provided unless explicitly opted in via capabilities.

```csharp
[JsonIgnore]
[Obsolete("Use HoverProviderUnion instead")]
public bool? HoverProvider
{
    get => HoverProviderUnion?.First;
    set => HoverProviderUnion = value;
}

[JsonPropertyName("hoverProvider")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public SumType<bool, HoverOptions>? HoverProviderUnion
{
    get;
    set;
}
```

### 2.  Update partners to new version.
Update Razor / XAML to consume the new union type property.

### 3.  Switch original property to use union type, obsolete intermediate property.
After partners have switched, we can now change the type of the original property and obsolete the intermediate one:
```csharp
[JsonPropertyName("hoverProvider")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public SumType<bool, HoverOptions>? HoverProvider
{
    get;
    set;
}

[JsonIgnore]
[Obsolete("Use HoverProvider instead")]
public SumType<bool, HoverOptions>? HoverProviderUnion
{
    get => HoverProvider?.First;
    set => HoverProvider = value;
}
```

### 4.  Delete the intermediate property.
After partners have switched again, we can delete the intermediate property.
```csharp
[JsonPropertyName("hoverProvider")]
[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
public SumType<bool, HoverOptions>? HoverProvider
{
    get;
    set;
}
```