System.Runtime.InteropServices.ExtendedLayoutAttribute Compiler Support
=======================================================================

The .NET runtime team is introducing a new attribute, `System.Runtime.InteropServices.ExtendedLayoutAttribute`, which will allow the runtime team to provide additional layout options for types, primarily for interop scenarios.
To provide the user experience requested by the runtime team, the C# and VB compilers will have the following support for this attribute:

- If a type has the `ExtendedLayoutAttribute` applied, the compiler will emit the `TypeAttributes.ExtendedLayout` value in the type's `TypeAttributes` flags.
- If a type has the `ExtendedLayoutAttribute` applied, the compiler will not allow the `StructLayoutAttribute` to be applied to the type.
- (C# compiler only) If a type has the `ExtendedLayoutAttribute` applied, the compiler will not allow the `InlineArrayAttribute` to be applied to the type.
- Within the Roslyn compiler, the `ITypeSymbol` for a type with the `ExtendedLayoutAttribute` applied will have the following behavior:
  - The `Layout` property will return a `TypeLayout` instance with the `LayoutKind` set to `Extended` (`1`), `Size` set to `0` and `Pack` set to `0`.
- A type that is embedded in an assembly (using the `NoPia` technology) will have the `ExtendedLayoutAttribute` preserved on the embedded type.

The Roslyn compiler will not have knowledge of the specific options available on the `ExtendedLayoutAttribute`, as these options will be defined by the runtime team and may expand over time. The compiler will not attempt to detect invalid field types for specific layout options. The runtime team may add analyzers in the future to handle these scenarios.

The compiler will only recognize the presence of the attribute and its implications for the type symbol's `LayoutKind` and the emitted attribute.
