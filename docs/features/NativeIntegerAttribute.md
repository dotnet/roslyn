Native Integer Metadata
=========
The following describes the representation of native integers in metadata.

## NativeIntegerAttribute
Type references are annotated in metadata with a `NativeIntegerAttribute`.

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
    public sealed class NativeIntegerAttribute : Attribute
    {
        public NativeIntegerAttribute()
        {
            TransformFlags = new[] { true };
        }
        public NativeIntegerAttribute(bool[] flags)
        {
            TransformFlags = flags;
        }
        public readonly bool[] TransformFlags;
    }
}
```

The `NativeIntegerAttribute` type is for compiler use only - it is not permitted in source.
The type declaration is synthesized by the compiler if not already included in the compilation.

Each type reference in metadata may have an associated `NativeIntegerAttribute` with a `bool[]` where each `bool`
represents whether the associated type is a native integer (`nint` or `nuint` in C#) or the underlying type (`System.IntPtr` or `System.UIntPtr`).
Custom modifiers associated with the type reference are ignored.

A type reference may have at most one `NativeIntegerAttribute`.
The attribute is not inherited.

The `bool[]` is constructed as follows:
- `System.IntPtr`, `System.UIntPtr`: `true` if the type represents a native integer; `false` otherwise
- Generic type: the representation of the type arguments in order including containing types
- Tuple: the representation of the underlying constructed type
- Array: the representation of the element type
- Pointer type: the representation of the pointed at type
- Function pointer type: the representation of the return type followed by the parameter types in order
- Other: none

Given the construction above, the number of elements in the `bool[]` matches the number of references to `System.IntPtr` and `System.UIntPtr` in the type.

## Optimizations

If the `bool[]` is empty, the `NativeIntegerAttribute` is omitted.

If the `bool[]` contains a single `true` value, the `NativeIntegerAttribute` uses the parameter-less constructor.

## Examples
```C#
System.IntPtr A;         // System.IntPtr A
nint B;                  // [NativeInteger] System.IntPtr B
IEnumerable<nuint> C     // [NativeInteger] System.Collections.Generic.IEnumerable<System.IntPtr> C
(System.IntPtr, nint) D; // [NativeInteger(new[] { false, true })] System.ValueTuple<System.IntPtr, System.IntPtr> D
```
