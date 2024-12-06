# String literals in data section

This opt-in **experimental** Roslyn feature allows changing how string literals in C# programs are emitted into PE files (`.dll`/`.exe`).
By default, string literals are emitted to the UserString heap which is limited to [2^24 bytes](https://github.com/dotnet/roslyn/issues/9852).
When the limit is reached, the following compiler error is reported by Roslyn:

```
error CS8103: Combined length of user strings used by the program exceeds allowed limit. Try to decrease use of string literals.
```

By turning on [the feature flag](#configuration), string literals ([where possible](#eligible-string-literals)) are instead emitted as UTF-8 data into a different section of the PE file
which does not have the same limit. The emit format is similar to [explicit u8 string literals][u8-literals].

The feature is currently implemented only for C#, not VB.

> [!WARNING]
> This feature is currently experimental and can be changed or removed at any time.

## Configuration

The feature flag can take a non-negative integer threshold.
Only string literals whose length (number of characters, not bytes) is greater than the threshold are emitted using the utf8 encoding strategy.
If the flag is set, but no value is specified, the threshold defaults to 100.
Specifying 0 means all non-empty string literals are considered for the feature.
Specifying `off` as the value turns the feature off (this is the default).

The feature flag can be specified on the command line like `/features:experimental-data-section-string-literals` or `/features:experimental-data-section-string-literals=20`,
or in a project file in a `<PropertyGroup>` like `<Features>$(Features);experimental-data-section-string-literals</Features>` or `<Features>$(Features);experimental-data-section-string-literals=20</Features>`.

## Eligible string literals

A string literal is emitted with the utf8 encoding if and only if all of these are true:
- it can be encoded as UTF-8, i.e., the following call does not throw:
  ```cs
  new System.Text.Encoding.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetBytes(literal)
  ```
- its `Length` is greater than the threshold specified by [the feature flag](#configuration).

## Emit

The standard string literal emit strategy emits `ldstr "string"`.
That puts the string literal on the UserString heap of the PE file.

The utf8 string literal encoding emit strategy emits `ldsfld` of a field in a generated class instead.

For every string literal, a unique internal static class is generated which:
- has name composed of `<S>` followed by a hex-encoded XXH128 hash of the string,
- is nested in the `<PrivateImplementationDetails>` type to avoid polluting the global namespace
  and to avoid having to enforce name uniqueness across modules,
- has one internal static readonly `string` field which is initialized in a static constructor of the class,
- is marked `beforefieldinit` so the static constructor can be called eagerly if deemed better by the runtime for some reason.

There is also an internal static readonly `.data` field generated into `<PrivateImplementationDetails>` containing the actual bytes,
similar to [u8 string literals][u8-literals] and [constant array initializers][constant-array-init].
These other scenarios might also reuse the data field, e.g., the following statements could all reuse the same data field:

```cs
ReadOnlySpan<byte> a = new byte[6] { 72, 101, 108, 108, 111, 46 };
ReadOnlySpan<byte> b = stackalloc byte[6] { 72, 101, 108, 108, 111, 46 };
ReadOnlySpan<byte> c = "Hello."u8;
string d = "Hello."; // assuming this string literal is eligible for the `ldsfld` emit strategy
```

The initialization calls `<PrivateImplementationDetails>.BytesToString` helper which in turn calls `Encoding.UTF8.GetBytes`.
This is an optimization so each of the generated class static constructors is slightly smaller in IL size.
These size savings can add up since one class is generated per one eligible string literal.
A compile-time error is reported if the `Encoding.UTF8.GetBytes` API is not available and needs to be used
(the user can then either ensure the API is available or turn off the feature flag).

The following example demonstrates the code generated for string literal `"Hello."`.

```cs
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[CompilerGenerated]
internal static class <PrivateImplementationDetails>
{
    internal static readonly __StaticArrayInitTypeSize=6 2BBCE396D68A9BBD2517F6B7064666C772694CD92010887BA379E10E7CDDA960 = /* IL: data(48 65 6C 6C 6F 2E) */;

    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 6)]
    internal struct __StaticArrayInitTypeSize=6
    {
    }
    
    internal static class <S>F6D59F18520336E6E57244B792249482
    {
        internal static readonly string s;

        unsafe static <S>F6D59F18520336E6E57244B792249482()
        {
            s = <PrivateImplementationDetails>.BytesToString((byte*)Unsafe.AsPointer(ref <PrivateImplementationDetails>.2BBCE396D68A9BBD2517F6B7064666C772694CD92010887BA379E10E7CDDA960), 6);
        }
    }

    internal unsafe static string BytesToString(byte* bytes, int length)
    {
        return Encoding.UTF8.GetString(bytes, length);
    }
}
```

### ILVerify and PEVerify

The actual generated code does not call `(byte*)Unsafe.AsPointer`.
It simply emits `ldsflda` of the data field.
The same approach is taken by other scenarios like [u8 string literals][u8-literals] and [constant array initializers][constant-array-init].
That works but does not pass IL/PE verification.

## Runtime

Throughput of `ldstr` vs `ldsfld` is very similar (both result in one or two move instructions).

In the `ldsfld` emit strategy, the `string` instances won't ever be collected by the GC once the generated class is initialized.
`ldstr` has similar behavior, but there are some optimizations in the runtime around `ldstr`,
e.g., they are loaded into a different frozen heap so machine codegen can be more efficient (no need to worry about pointer moves).

Generating new types by the compiler means more type loads and hence runtime impact,
e.g., startup performance and the overhead of keeping track of these types.

The generated types are returned from reflection like `Assembly.GetTypes()`
which might impact the performance of Dependency Injection and similar systems.

> [!NOTE]
> In practice, there might not be many generated types, it depends on the kind of the program
> (whether it has lots of short strings or a few large strings) and how [the threshold](#configuration) is configured.

## Implementation

`CodeGenerator` obtains [configuration of the feature flag](#configuration) from `Compilation` passed to its constructor.
`CodeGenerator.EmitConstantExpression` switches to the utf8 encoding strategy for [eligible string literals](#eligible-string-literals).
That uses the following API responsible for synthesizing the `<S>` class and returning reference to its `string` field:

```cs
IFieldReference ITokenDeferral.GetFieldForDataString(ImmutableArray<byte> data, SyntaxNode syntaxNode, DiagnosticBag diagnostics)
```

Similar to `ITokenDeferral.GetFieldForData`, this calls into `PrivateImplementationDetails`
which is responsible for synthesizing the classes and helpers in a thread-safe and deterministic manner.

There are implementations of various `Cci` interfaces which represent the metadata,
rooted in a `DataStringHolder` which is the `INestedTypeDefinition` for the `<S>` class.

These are implemented in `Microsoft.CodeAnalysis`.
The alternative (used by inline arrays, for example)
would be to implement the `Symbol`s in the language-specific projects (and then use the standard `Cci` adapters),
but that seems to require similar amount of implemented abstract properties/methods of the `Symbol`
as the implementations of `Cci` interfaces require.
But implementing `Cci` directly allows us to reuse the same implementation for VB if needed in the future.

## Future work

### Edit and Continue

Hot reload currently does not support `.data` field replacement.
That can be implemented in the future.

### AOT

Ahead-of-time compilation tools would need to be updated to recognize this new pattern of string literal use in place of `ldstr`.

### Ref assemblies

Need to confirm that no additional work is needed for ref assemblies.

### Automatic threshold

The threshold could be determined automatically with some objective, for example,
use the utf8 encoding emit strategy for the lowest number of string literals necessary to avoid overflowing the UserString heap.

The set of string literals is not know up front in the compiler, it is discovered lazily (and in parallel) by the emit layer.
However, we could continue emitting `ldstr` instructions and fix them up in a separate phase after we have seen all the string literals
(and hence can determine the automatic threshold).
The `ldstr` and `ldsfld` instructions have the same size, so fixup would be a straightforward replace.
This fixup phase already exists in the compiler in `MetadataWriter.WriteInstructions`
(it's also how the string literals are emitted into the UserString heap).
It is called from `SerializeMethodBodies` which precedes `PopulateSystemTables` call,
hence synthesizing the utf8 string classes in the former should be possible and they would be emitted in the latter.

### Statistics

The compiler could emit an info diagnostic with useful statistics for customers to determine what threshold to set.
For example, a histogram of string literal lengths encountered while emitting of the program.

### Single blob

We could merge all UTF-8 byte sequences for affected strings into a single blob - a virtual User-String heap.
The sequences can be deduped as we dedupe entries in User-String heap.
We can even attempt to do some vary basic compression,
for example, in the name heap that we generate (contains type and member names, etc.), names with shared content can overlap.
We would generate a single `__StaticArrayInitTypeSize=*` structure for the entire virtual heap blob and
add a single `.data` field to `<PrivateImplementationDetails>` that points to the blob.
At runtime, we would do an offset to where the required data reside in the blob and decode the required length from UTF-8 to UTF-16.

## Alternatives

### Configuration/emit granularity

Instead of one global feature flag, the emit strategy could be controlled using compiler-recognized attributes (applicable to assemblies or classes).
Furthermore, we could emit more than one string per one class. That could be configurable as well.

### GC

To avoid rooting the `string` references forever, we could turn the fields into `WeakReference<string>`s.
Or we could avoid the caching altogether (each eligible `ldstr` would be replaced with a direct call to `Encoding.UTF8.GetString`).
This could be configurable as well.

### Runtime support

We could emit the strings to data fields similarly but we would not synthesize the `string` fields and static constructors.
Instead, at the place of `ldstr`, we would directly call a runtime-provided helper, passing a pointer to the data field.
That runtime helper would be an intrinsic recognized by the JIT (with a fallback calling `Encoding.UTF8.GetString`).

### Custom lazy initialization

All the data and string fields could be part of one class, e.g., `<PrivateImplementationDetails>`,
and the compiler could emit custom lazy initialization instead of relying on static constructors, something like:

```cs
static class <PrivateImplementationDetails>
{
    private static string _str1;
    internal static string Str1 => _str1 ??= Load(str1DataSection);
}
```

However, that would likely result in worse machine code due to more branches and function calls.

<!-- links -->
[u8-literals]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/utf8-string-literals
[constant-array-init]: https://github.com/dotnet/roslyn/pull/24621
