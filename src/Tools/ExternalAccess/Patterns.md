# External Access Patterns

## Naming

### Prefixes

* Top-level interfaces have the prefix `IPartner`
* Other top-level types have the prefix `Partner`

### Suffixes

* Wrappers for internal Roslyn data types have the suffix `Wrapper`
* Interfaces for exposing internal Roslyn services to partner code have the suffix `Accessor`

## Attributes

### `[ExternalApi]`

This attribute may be applied to a type or member which is marked `internal` intended to be visible to partner code. An analyzer for this attribute will report a compilation error if the same type would fail to compile if it was marked `public`. The attribute helps authors of the external access layer write types that can be consumed successfully by partner code.

> 💡 This attribute only needs to be applied to `internal` types and members. The analysis of this attribute will automatically consider all `public` nested types and members of the item marked `[ExternalApi]`.

### `[LinkedEnumeration]`

This attribute may be applied to an `enum` type which mirrors an internal Roslyn enumeration. For example, `CodeLensGlyph` mirrors `Glyph`. An analyzer for this attribute will report a compilation error if the original enumeration changes or becomes out-of-sync with the definition in the external access layer.

## Wrapper types

To reduce allocations, wrappers for internal Roslyn types are always defined as value types.

| Roslyn type | Wrapper type | Wrapper usage |
| --- | --- | --- |
| `class T` | `readonly struct TWrapper` | `TWrapper?` |
| `readonly struct T` | `readonly struct TWrapper` | `TWrapper` |
| `struct T` | `struct TWrapper` | `TWrapper` |

Wrappers have the following members:

* Zero or more `public` constructors allowing partner code to construct the Roslyn object
* One `internal` constructor allowing code in the external access layer to wrap an existing Roslyn object
* A property `UnderlyingObject` allowing the external access layer to unwrap an existing Roslyn object

```csharp
[ExternalApi]
internal readonly struct PartnerRoslynTypeWrapper {
    // Use public constructors if the partner needs to construct the Roslyn object
    public PartnerRoslynTypeWrapper(T1 arg1, T2 arg2, ...)
        => UnderlyingObject = new RoslynType(arg1, arg2, ...);

    // Include this internal constructor if the partner needs to consume the object constructed by Roslyn
    internal PartnerRoslynTypeWrapper(RoslynType underlyingObject)
        => UnderlyingObject = underlyingObject ?? throw new ArgumentNullException(nameof(underlyingObject));

    internal RoslynType UnderlyingObject { get; }

    // Define 'public' members here which expose functionality of RoslynType
}
```

## Accessing MEF exports

> 💡 This section applies to `internal` Roslyn types which are MEF-exported using a "normal" export attribute. Workspace and language services are covered separately.

```csharp
[ExternalApi]
internal interface IPartnerServiceNameAccessor {
    // Define members for accessing IServiceName functionality
}

[Export(typeof(IPartnerServiceNameAccessor))]
[Shared]
internal sealed class PartnerServiceNameAccessor : IPartnerServiceNameAccessor {
    private readonly IServiceName _implementation;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PartnerServiceNameAccessor(IServiceName implementation) {
        _implementation = implementation;
    }

    // Implement IPartnerServiceNameAccessor by delegating to _implementation
}
```

## Accessing workspace services

```csharp
[ExternalApi]
internal interface IPartnerServiceNameAccessor : IWorkspaceService {
    // Define members for accessing IServiceName functionality
}

internal sealed class PartnerServiceNameAccessor : IPartnerServiceNameAccessor {
    private readonly IServiceName _implementation;

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    public PartnerServiceNameAccessor(IServiceName implementation) {
        _implementation = implementation;
    }

    // Implement IPartnerServiceNameAccessor by delegating to _implementation
}

[ExportWorkspaceServiceFactory(typeof(IPartnerServiceNameAccessor))]
[Shared]
internal sealed class PartnerServiceNameAccessorFactory : IWorkspaceServiceFactory {
    private readonly IServiceName _implementation;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PartnerServiceNameAccessorFactory(IServiceName implementation) {
        _implementation = implementation;
    }

    [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices) {
        var implementation = workspaceServices.GetRequiredService<IServiceName>();
        return new PartnerServiceNameAccessor(implementation);
    }
}
```

## Providing MEF services

The partner service is exported in the external access layer, and depends on the partner separately providing the implementation of the service.

```csharp
/* These items are defined in the external access layer
 */
[ExternalApi]
internal interface IPartnerServiceImplementation {
    // Define members for the implementation of IService
}

[Export(typeof(IService))]
[Shared]
internal sealed class PartnerService : IService {
    private readonly IPartnerServiceImplementation _implementation;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public PartnerService(IPartnerServiceImplementation implementation)
        => _implementation = implementation;

    // Implement members of IService by delegating to _implementation
}
```

```csharp
/* This item is defined in partner code, separate from the external access layer
 */
[Export(typeof(IPartnerServiceImplementation))]
[Shared]
internal sealed class PartnerServiceImplementation : IPartnerServiceImplementation {
}
```
