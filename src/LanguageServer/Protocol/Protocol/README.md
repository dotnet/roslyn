## Summary
The files in this folder defines C# types for [LSP protocol definitions](https://microsoft.github.io/language-server-protocol/specifications/lsp/3.17/specification/) and custom VS LSP protocol definitions.

These types are shared via restricted IVTs to Razor and XAML, as they run inside the C# Roslyn LSP server.

## Breaking Changes
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