# String literals in data section

This opt-in Roslyn feature allows changing how string literals in C# programs are emitted into PE files (`.dll`/`.exe`).
By default, string literals are emitted to the UserString heap which is limited to [2^24 bytes](https://github.com/dotnet/roslyn/issues/9852).
When the limit is reached, the following compiler error is reported by Roslyn:

```
error CS8103: Combined length of user strings used by the program exceeds allowed limit. Try to decrease use of string literals.
```

By turning on [the feature flag](#configuration), string literals ([where possible](#eligible-string-literals)) are instead emitted as UTF-8 data into a different section of the PE file
which does not have the same limit. The emit format is similar to [explicit u8 string literals][u8-literals].

The feature is currently implemented only for C#, not VB.

## Configuration

The feature flag can take a non-negative integer threshold. Only string literals whose length is over the threshold are emitted using the utf8 encoding strategy.
By default, the threshold is 100. Specifying 0 means all string literals are considered for the feature. Specifying `off` turns off the feature (this is the default).

The feature flag can be specified on the command line like `/features:utf8-string-literal-encoding` or `/features:utf8-string-literal-encoding=20`,
or in a project file in a `<PropertyGroup>` like `<Features>$(Features);utf8-string-literal-encoding</Features>` or `<Features>$(Features);utf8-string-literal-encoding=20</Features>`.

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

For every string literal, a unique internal static class is generated which
- has name composed of `<S>` followed by a hex-encoded SHA-256 hash of the string,
- lives in the global namespace,
- has one `.data` field which is emitted the same way as for [explicit u8 string literals][u8-literals],
- has one `string` field which is initialized in a static constructor of the class.

The initialization calls `<PrivateImplementationDetails>.BytesToString` helper which in turn calls `Encoding.UTF8.GetBytes`.
This is an optimization so each of the generated class static constructors is slightly smaller in IL size.
These size savings can add up since one class is generated per one eligible string literal.

The following example demonstrates the code generated for string literal `"Hello."`.

```cs
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[CompilerGenerated]
internal static class <S>2D8BD7D9BB5F85BA643F0110D50CB506A1FE439E769A22503193EA6046BB87F7
{
    internal static readonly <PrivateImplementationDetails>.__StaticArrayInitTypeSize=6 f = /* IL: data(48 65 6C 6C 6F 2E) */;

    internal static readonly string s;

    unsafe static <S>2D8BD7D9BB5F85BA643F0110D50CB506A1FE439E769A22503193EA6046BB87F7()
    {
        s = <PrivateImplementationDetails>.BytesToString((byte*)Unsafe.AsPointer(ref f), 6);
    }
}

[CompilerGenerated]
internal static class <PrivateImplementationDetails>
{
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 6)]
    internal struct __StaticArrayInitTypeSize=6
    {
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
That works but does not pass IL/PE verification.

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
rooted in a `DataStringHolder` which is the `INamespaceTypeDefinition` for the `<S>` class.

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

### Configuration/emit alternatives

Instead of one global feature flag, the emit strategy could be controlled using compiler-recognized attributes (applicable to assemblies or classes).
Furthermore, we could emit more than one string per one class. That could be configurable as well.

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

## Alternatives

### Runtime support

We could emit the strings to data fields similarly but we would not synthesize the `string` fields and static constructors.
Instead, at the place of `ldstr`, we would directly call a runtime-provided helper, passing a pointer to the data field.
That runtime helper would be an intrinsic recognized by the JIT (with a fallback calling `Encoding.UTF8.GetString`).

<!-- links -->
[u8-literals]: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/utf8-string-literals
