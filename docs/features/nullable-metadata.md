Nullable Metadata
=========
The following describes the representation of nullable annotations in metadata.

## NullableAttribute
Type references are annotated in metadata with a `NullableAttribute`.

```C#
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Event |
        AttributeTargets.Field |
        AttributeTargets.GenericParameter |
        AttributeTargets.Parameter |
        AttributeTargets.Property |
        AttributeTargets.ReturnValue,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;
        public NullableAttribute(byte flag)
        {
            NullableFlags = new byte[] { flag };
        }
        public NullableAttribute(byte[] flags)
        {
            NullableFlags = flags;
        }
    }
}
```

The `NullableAttribute` type is for compiler use only - it is not permitted in source.
The type declaration is synthesized by the compiler if not already included in the compilation.

Each type reference in metadata may have an associated `NullableAttribute` with a `byte[]` where each `byte`
represents nullability: 0 for oblivious, 1 for not annotated, and 2 for annotated.

The `byte[]` is constructed as follows:
- Reference type: the nullability (0, 1, or 2), followed by the representation of the type arguments in order including containing types
- Nullable value type: the representation of the type argument only
- Non-generic value type: skipped
- Generic value type: 0, followed by the representation of the type arguments in order including containing types
- Array: the nullability (0, 1, or 2), followed by the representation of the element type
- Tuple: the representation of the underlying constructed type
- Type parameter reference: the nullability (0, 1, or 2, with 0 for unconstrained type parameter)

Note that non-generic value types are represented by an empty `byte[]`.
However, generic value types and type parameters constrained to value types have an explicit 0 in the `byte[]` for nullability.
The reason generic types and type parameters are represented with an explicit `byte` is to simplify metadata import.
Specifically, this avoids the need to calculate whether a type parameter is constrained to a value type when
decoding nullability metadata, since the constraints may include a (valid) cyclic reference to the type parameter.

### Optimizations

If the `byte[]` is empty, the `NullableAttribute` is omitted.

If all values in the `byte[]` are the same, the `NullableAttribute` is constructed with that single `byte` value. (For instance, `NullableAttribute(1)` rather than `NullableAttribute(new byte[] { 1, 1 }))`.)

### Type parameters
Each type parameter definition may have an associated `NullableAttribute` with a single `byte`:

1. `notnull` constraint: `NullableAttribute(1)`
2. `class` constraint in `#nullable disable` context: `NullableAttribute(0)`
3. `class` constraint in `#nullable enable` context: `NullableAttribute(1)`
4. `class?` constraint: `NullableAttribute(2)`
5. No `notnull`, `class`, `struct`, `unmanaged`, or type constraints in `#nullable disable` context: `NullableAttribute(0)`
6. No `notnull`, `class`, `struct`, `unmanaged`, or type constraints in `#nullable enable` context
(equivalent to an `object?` constraint): `NullableAttribute(2)`

## NullableContextAttribute
`NullableContextAttribute` can be used to indicate the nullability of type references that have no `NullableAttribute` annotations.

```C#
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.Delegate |
        AttributeTargets.Interface |
        AttributeTargets.Method |
        AttributeTargets.Struct,
        AllowMultiple = false,
        Inherited = false)]
    public sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;
        public NullableContextAttribute(byte flag)
        {
            Flag = flag;
        }
    }
}
```

The `NullableContextAttribute` type is for compiler use only - it is not permitted in source.
The type declaration is synthesized by the compiler if not already included in the compilation.

The `NullableContextAttribute` is optional - nullable annotations can be represented in metadata with full fidelity using `NullableAttribute` only.

`NullableContextAttribute` is valid in metadata on type and method declarations.
The `byte` value represents the implicit `NullableAttribute` value for type references within that scope
that do not have an explicit `NullableAttribute` and would not otherwise be represented by an empty `byte[]`.
The nearest `NullableContextAttribute` in the metadata hierarchy applies.
If there are no `NullableContextAttribute` attributes in the hierarchy,
missing `NullableAttribute` attributes are treated as `NullableAttribute(0)`.

The attribute is not inherited.

The C#8 compiler uses the following algorithm to determine which `NullableAttribute` and
`NullableContextAttribute` attributes to emit.
First, `NullableAttribute` attributes are generated at each type reference and type parameter definition by:
calculating the `byte[]`, skipping empty `byte[]`, and collapsing `byte[]` to single `byte` where possible.
Then at each level in metadata hierarchy starting at methods:
The compiler finds the most common single `byte` value across all the `NullableAttribute` attributes at that level
and any `NullableContextAttribute` attributes on immediate children.
If there are no single `byte` values, there are no changes.
Otherwise, a `NullableContext(value)` attribute is created at that level where `value` is most common
value (preferring `0` over `1` and preferring `1` over `2`), and all `NullableAttribute` and `NullableContextAttribute` attributes with that value are removed.
That iterative process continues up to, and including, the top-level containing type definition.
If the common value at the top-level type definition is a value other than `0` (the default),
a `NullableContext(value)` attribute is emitted.

Note that an assembly compiled with C#8 where all reference types are oblivious will have no
`NullableContextAttribute` and no `NullableAttribute` attributes emitted.
That is equivalent to a legacy assembly.

### Examples
```C#
// C# representation of metadata
[NullableContext(2)]
class Program
{
    string s;                        // string?
    [Nullable({ 2, 1, 2 }] Dictionary<string, object> d; // Dictionary<string!, object?>?
    [Nullable(1)] int[] a;           // int[]!
    int[] b;                         // int[]?
    [Nullable({ 0, 2 })] object[] c; // object?[]~
}
```

## Private members

To reduce the size of metadata, the C#8 compiler can be configured to not emit attributes
for members that are inaccessible outside the assembly (`private` members, and also `internal` members
if the assembly does not contain `InternalsVisibleToAttribute` attributes).

The compiler behavior is configured from a command-line flag.
For now a feature flag is used: `-features:nullablePublicOnly`.

If private member attributes are dropped, the compiler will emit a `[module: NullablePublicOnly]` attribute.
The presence or absence of the `NullablePublicOnlyAttribute` can be used by tools to interpret
the nullability of private members that do not have an associated `NullableAttribute` attribute.

For members that do not have explicit accessibility in metadata
(specifically for parameters, type parameters, events, and properties),
the compiler uses the accessibility of the container to determine whether to emit nullable attributes. 

```C#
namespace System.Runtime.CompilerServices
{
    [System.AttributeUsage(AttributeTargets.Module, AllowMultiple = false)]
    public sealed class NullablePublicOnlyAttribute : Attribute
    {
        public readonly bool IncludesInternals;
        public NullablePublicOnlyAttribute(bool includesInternals)
        {
            IncludesInternals = includesInternals;
        }
    }
}
```

The `NullablePublicOnlyAttribute` type is for compiler use only - it is not permitted in source.
The type declaration is synthesized by the compiler if not already included in the compilation.

`IncludesInternal` is true if `internal` members are annotated in addition to `public` and `protected` members.

## Compatibility

The nullable metadata does not include an explicit version number.
Where possible, the compiler will silently ignore attribute forms that are unexpected.

The metadata format described here is incompatible with the format used by earlier C#8 previews:
1. Concrete non-generic value types are no longer included in the `byte[]`, and
2. `NullableContextAttribute` attributes are used in place of explicit `NullableAttribute` attributes.

Those differences mean that assemblies compiled with earlier previews may be read incorrectly by later previews,
and assemblies compiled with later previews may be read incorrectly by earlier previews.
