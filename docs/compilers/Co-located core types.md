Co-located core types
=====================

## Customer Scenario and Context

The ECMA 335 spec indicates that a few types, such as `System.Object`, or `System.Int16`, can be referenced in metadata as an integer value rather than a type ref token (built-in types). Additionally, the spec indicates that certain conversions are allowed between these types and a small set of token-referenced types – we shall refer to these as special types. Finally, a few types have special runtime treatment without being referenced like the other built-in types.

Scenarios

1. A compiler needs to find the type definition for a built-in type, given a reference to it. Given that the reference is not a type token, the compiler needs to know the assembly containing the def

2. A compiler encounters two definitions for a built-in type

3. A compiler is asked to type check two references to a built-in type, one of which is originating from a library compiled to an incompatible definition of core types

## Design

Compilers may assume that there is exactly one assembly representing the "core assembly". This core assembly is either referenced or is currently being compiled. The core assembly must not reference any other assemblies.

The core assembly contains the type definitions for the subset of built-in and special types supported on a platform.

References to types having the same name as built-in types but found outside the core assembly are made via type tokens.

An assembly `A` referencing only built-in types may exclude (as per ECMA spec) a reference to the core library. In the case `A` is further referenced by the compiler when compiling a second assembly `B`, the references to the built-in types in `A` are resolved by the compiler based on the core library used for compiling `B`.

A platform implementation may choose to define a core assembly with only a subset of the built-in and special types. Compilation in such a scenario succeeds, as long as built-in or special types outside of the subset are not referenced, directly (by user code) or indirectly (by compiler features). This means that, while the compiler can safely assume that all built-in and special types are co-located in the same assembly, they may not all be present.

If a later version of the platform introduces more built-in or special types, they must do so in the core assembly.

Correspondingly, a runtime for such a platform need only support the subset in the corresponding core assembly.

The list of co-located types is exactly the following:

- `System.Object`
- `System.Enum`
- `System.MulticastDelegate`
- `System.Delegate`
- `System.ValueType`
- `System.Void`
- `System.Boolean`
- `System.Char`
- `System.SByte`
- `System.Byte`
- `System.Int16`
- `System.UInt16`
- `System.Int32`
- `System.UInt32`
- `System.Int64`
- `System.UInt64`
- `System.Decimal`
- `System.Single`
- `System.Double`
- `System.String`
- `System.IntPtr`
- `System.UIntPtr`
- `System.Array`
- `System.DateTime`
- `System.Collections.IEnumerable`
- `System.Collections.Generic.IEnumerable<>`
- `System.Collections.Generic.IList<>`
- `System.Collections.Generic.ICollection<>`
- `System.Collections.Generic.IEnumerator<>`
- `System.Collections.IEnumerator`
- `System.Nullable<>`
- `System.Runtime.CompilerServices.IsVolatile`
- `System.IDisposable`
- `System.TypedReference`
- `System.IAsyncResult`
- `System.AsyncCallback`
- `System.Collections.Generic.IReadOnlyList<>`
- `System.Collections.Generic.IReadOnlyCollection<>`

We continue to assume that references to `System.Type` are made by-enumeration value when the reference is made in a custom attribute, and as a regular reference when not. Note the departure from the ECMA spec which included `System.Type`. `System.Type` and other reflection types are intentionally not in the core library. Since `System.Type` may be referenced as a built-in type, the compiler may assume that there is at most one definition for this type available in the compilation context, and treat as an error the case where there is more than one. No definition should be accepted as long as there is no reference.

Language features relying on APIs not in the list should not assume any particular assembly location for the definition of such APIs. Compilers should either use heuristics, or require explicit specification of the location of such APIs.
