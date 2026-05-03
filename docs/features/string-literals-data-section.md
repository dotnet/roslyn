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
If the flag is set, but no value or an unrecognized value is specified, the threshold defaults to 100.
Specifying 0 means all non-empty string literals are considered for the feature.

The feature is turned off if
- the feature flag is not specified, or
- the string `off` is provided as the feature flag value, or
- `null` is provided as the feature flag value (usually only possible using Roslyn APIs like `ParseOptions.WithFeatures`).

The feature flag can be specified on the command line like `/features:experimental-data-section-string-literals` or `/features:experimental-data-section-string-literals=20`,
or in a project file in a `<PropertyGroup>` like `<Features>$(Features);experimental-data-section-string-literals</Features>` or `<Features>$(Features);experimental-data-section-string-literals=20</Features>`.

> [!NOTE]
> This configuration is useful for experimenting (to see the impact of the threshold on runtime performance and IL size)
> and also allowing users to avoid a potential overhead of many type definitions generated if their program has many short string literals
> which do not need this new emit strategy because they can fit into the size-limited UserString heap.

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

For every unique string literal, a unique internal static class is generated which:
- has name composed of `<S>` followed by a hex-encoded XXH128 hash of the string
  (collisions [should not happen][xxh128] with XXH128, but if there are string literals which would result in the same XXH128 hash, a compile-time error is reported),
- is nested in the `<PrivateImplementationDetails>` type to avoid polluting the global namespace
  and to avoid having to enforce name uniqueness across modules,
- has one internal static readonly `string` field which is initialized in a static constructor of the class,
- is marked `beforefieldinit` so the static constructor can be called eagerly if deemed better by the runtime for some reason.

There is also an internal static readonly `.data` field generated into `<PrivateImplementationDetails>` containing the actual bytes,
similar to [u8 string literals][u8-literals] and [constant array initializers][constant-array-init].
This field uses hex-encoded SHA-256 hash for its name and collisions are currently not reported by the compiler.
These other scenarios might also reuse the data field, e.g., the following statements could all reuse the same data field:

```cs
ReadOnlySpan<byte> a = new byte[6] { 72, 101, 108, 108, 111, 46 };
ReadOnlySpan<byte> b = stackalloc byte[6] { 72, 101, 108, 108, 111, 46 };
ReadOnlySpan<byte> c = "Hello."u8;
string d = "Hello."; // assuming this string literal is eligible for the `ldsfld` emit strategy
```

The initialization calls `<PrivateImplementationDetails>.BytesToString` helper which in turn calls `Encoding.UTF8.GetString`.
This is an optimization so each of the generated static constructors is slightly smaller in IL size.
These size savings can add up since one class is generated per one eligible string literal.
A compile-time error is reported if the `Encoding.UTF8.GetString` API is not available and needs to be used
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

    private unsafe static string BytesToString(byte* bytes, int length)
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

### Ref assemblies

The synthesized types are not part of ref assemblies. That makes them smaller
and incremental compilation is faster because it does not need to recompile dependent projects when only string literal contents change.

This is automatically implemented, because during metadata-only compilation, method bodies are not emitted,
and the synthesized types used by this feature are only emitted lazily as part of generating method body instructions.

## Diagnostics

The error CS8103 (Combined length of user strings used by the program exceeds allowed limit) is updated to suggest the feature flag,
albeit with a disclaimer during the experimental phase of the feature.

## Runtime

Throughput of `ldstr` vs `ldsfld` is very similar (both result in one or two move instructions).

In the `ldsfld` emit strategy, the `string` instances won't ever be collected by the GC once the generated class is initialized.
`ldstr` has similar behavior (GC does not collect the string literals either until the assembly is unloaded),
but there are some optimizations in the runtime around `ldstr`,
e.g., they are loaded into a different frozen heap so machine codegen can be more efficient (no need to worry about pointer moves).

Generating new types by the compiler means more type loads and hence runtime impact,
e.g., startup performance and the overhead of keeping track of these types.
On the other hand, the PE size might be smaller due to UTF-8 vs UTF-16 encoding,
which can result in memory savings since the binary is also loaded to memory by the runtime.
See [below](#runtime-overhead-benchmark) for a more detailed analysis.

The generated types are returned from reflection like `Assembly.GetTypes()`
which might impact the performance of Dependency Injection and similar systems.

### Runtime overhead benchmark

| [cost per string literal](https://github.com/jkotas/stringliteralperf) | feature on | feature off |
| --- | --- | --- |
| bytes | 1037 | 550 |
| microseconds | 20.3 | 3.1 |

The benchmark results above [show](https://github.com/dotnet/roslyn/pull/76139#discussion_r1944144978)
that the runtime overhead of this feature per 100 char string literal
is ~500 bytes of working set memory (~2x of regular string literal)
and ~17 microseconds of startup time (~7x of regular string literal).

The startup time overhead does depend on the length of the string literal.
It is cost of the type loads and JITing the static constructor.

The working set has two components: private working set (r/w pages) and non-private working set (r/o pages backed by the binary).
The private working set overhead (~600 bytes) does not depend on the length of the string literal.
Again, it is the cost of the type loads and the static constructor code.
Non-private working set is reduced by this feature since the binary is smaller.
Once the string literal is about 600 characters,
the private working set overhead and non-private working set improvement will break even.
For string literals longer than 600 characters, this feature is total working set improvement.

<details>
<summary>Why 600 bytes?</summary>

When the feature is off, ~550 bytes cost of 100 char string literal is composed from:
- The string in the binary (~200 bytes).
- The string allocated on the GC heap (~200 bytes).
- Fixed overheads: metadata encoding, runtime hashtable of all allocated string literals, code that referenced the string in the benchmark (~150 bytes).

When the feature is on, ~1050 bytes cost of 100 char string literal is composed from:
- The string in the binary (~100 bytes).
- The string allocated on the GC heap (~200 bytes).
- Fixed overheads: metadata encoding, the extra types, code that referenced the string in the benchmark (~750 bytes).

750 - 150 = 600. Vast majority of it are the extra types.

A bit of the extra fixed overheads with the feature on is probably in the non-private working set.
It is difficult to measure it since there is no managed API to get private vs. non-private working set.
It does not impact the estimate of the break-even point for the total working set.

</details>

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

## Future work and alternatives

### Edit and Continue

Hot reload currently does not support `.data` field replacement.
That can be implemented in the future.

### AOT

Ahead-of-time compilation tools would need to be updated to recognize this new pattern of string literal use in place of `ldstr`.

### Automatic threshold

The threshold could be determined automatically with some objective, for example,
use the utf8 encoding emit strategy for the lowest number of string literals necessary to avoid overflowing the UserString heap.

The set of string literals is not known up front in the compiler, it is discovered lazily (and in parallel) by the emit layer.
However, we could continue emitting `ldstr` instructions and fix them up in a separate phase after we have seen all the string literals
(and hence can determine the automatic threshold).
The `ldstr` and `ldsfld` instructions have the same size, so fixup would be a straightforward replace.
This fixup phase already exists in the compiler in `MetadataWriter.WriteInstructions`
(it's also how the string literals are emitted into the UserString heap).
It is called from `SerializeMethodBodies` which precedes `PopulateSystemTables` call,
hence synthesizing the utf8 string classes in the former should be possible and they would be emitted in the latter.

Alternatively, we could collect string literals during binding, then before emit sort them by length and content (for determinism)
to find the ones that are over the threshold and should be emitted with this new strategy.

### Statistics

The compiler could emit an info diagnostic with useful statistics for customers to determine what threshold to set.
For example, a histogram of string literal lengths encountered while emitting the program.
Tools like ILSpy can already be used to obtain some related data, e.g., count, length, and contents of string literals in the UserString heap.

### Single blob

We could merge all UTF-8 byte sequences for affected strings into a single blob - a virtual User-String heap.
The sequences can be deduped as we dedupe entries in User-String heap.
We can even attempt to do some vary basic compression,
for example, in the name heap that we generate (contains type and member names, etc.), names with shared content can overlap.
We would generate a single `__StaticArrayInitTypeSize=*` structure for the entire virtual heap blob and
add a single `.data` field to `<PrivateImplementationDetails>` that points to the blob.
At runtime, we would do an offset to where the required data reside in the blob and decode the required length from UTF-8 to UTF-16.

However, this would be unfriendly to IL trimming.

### Configuration/emit granularity

Instead of one global feature flag, the emit strategy could be controlled using compiler-recognized attributes (applicable to assemblies or classes).
Furthermore, we could emit more than one string per one class. That could be configurable as well.

One interesting strategy would be grouping strings which occur in one user-defined type into one compiler-generated `<S>` type.
The idea is that strings from one class are likely used "together" so there is no performance impact from initializing them all at once and we save on metadata.

### GC

To avoid rooting the `string` references forever, we could turn the fields into `WeakReference<string>`s
(note that this would be quite expensive for both direct overhead and indirectly for the GC due to longer GC pause times).
Or we could avoid the caching altogether (each eligible `ldstr` would be replaced with a direct call to `Encoding.UTF8.GetString`).
This could be configurable as well.

### Runtime support

We could emit the strings to data fields similarly but we would not synthesize the `string` fields and static constructors.
Instead, at the place of `ldstr`, we would directly call a runtime-provided helper, passing a pointer to the data field.
That runtime helper would be an intrinsic recognized by the JIT (with a fallback calling `Encoding.UTF8.GetString`).
The runtime would then be responsible for loading and caching the strings, perhaps similarly to how `ldstr` works (using a frozen GC heap).

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

### String interning

The compiler should report a diagnostic when the feature is enabled together with
`[assembly: System.Runtime.CompilerServices.CompilationRelaxations(0)]`, i.e., string interning enabled,
because that is incompatible with the feature.

### Avoiding hash collisions

Instead of XXH128 for the type names and SHA-256 for the data field names, we could use index-based names.
- The compiler could assign names lazily based on metadata tokens which are deterministic.
  If building on the current approach, that might require some refactoring,
  because internal data structures in the compiler might not be ready for lazy names like that.
  But it would be easier if combined with the first strategy suggested for [automatic threshold](#automatic-threshold) above,
  where we would not synthesize the types until very late in the emit phase (during fixup of the metadata tokens).
- We could build on the second strategy suggested for [automatic threshold](#automatic-threshold) where we would collect string literals during binding
  (and perhaps also constant arrays and u8 strings if we want to extend this support to them as well),
  then before emit we would sort them by length and content and assign indices to them to be then used for the synthesized names.

<!-- links -->
[u8-literals]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/utf8-string-literals
[constant-array-init]: https://github.com/dotnet/roslyn/pull/24621
[xxh128]: https://github.com/Cyan4973/xxHash/blob/86f6400a2a14ea7123ada691c89faf1d2a6a126f/tests/collisions/README.md
