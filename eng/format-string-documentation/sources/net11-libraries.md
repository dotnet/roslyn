---
title: What's new in .NET libraries for .NET 11
description: Learn about the updates to the .NET libraries for .NET 11.
titleSuffix: ""
ms.date: 10/13/2026
ai-usage: ai-assisted
ms.update-cycle: 3650-days
---

# What's new in .NET libraries for .NET 11

This article describes new features in the .NET libraries for .NET 11. It was last updated for release candidate 2 (RC 2).

## Diagnostics and process execution

- [Process API expansion](#process-api-expansion)
- [Console FORCE_COLOR support](#console-force_color-support)

### Process API expansion

<xref:System.Diagnostics.Process> has a substantial set of new APIs that cover common scenarios where you previously had to wire up <xref:System.Diagnostics.Process.OutputDataReceived>/<xref:System.Diagnostics.Process.ErrorDataReceived> events manually or use P/Invoke.

#### Run-and-capture helpers

New one-shot APIs let you launch a process and get its result without manual setup:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="ProcessRunAndCapture":::

The full set of helpers includes:

- <xref:System.Diagnostics.Process.Run*?displayProperty=nameWithType> and <xref:System.Diagnostics.Process.RunAsync*?displayProperty=nameWithType> — launch a process and return an exit-status result.
- <xref:System.Diagnostics.Process.RunAndCaptureText*?displayProperty=nameWithType> and <xref:System.Diagnostics.Process.RunAndCaptureTextAsync*?displayProperty=nameWithType> — launch and capture stdout/stderr together with exit code.
- <xref:System.Diagnostics.Process.ReadAllText(System.Nullable{System.TimeSpan})?displayProperty=nameWithType>, <xref:System.Diagnostics.Process.ReadAllBytes(System.Nullable{System.TimeSpan})?displayProperty=nameWithType>, and their async variants — read a child process's standard output in a single call.
- <xref:System.Diagnostics.Process.ReadAllLines(System.Nullable{System.TimeSpan})?displayProperty=nameWithType> — synchronously reads all lines of output from a child process, returning a sequence of <xref:System.Diagnostics.ProcessOutputLine> values that distinguish stdout from stderr.
- <xref:System.Diagnostics.Process.ReadAllLinesAsync(System.Threading.CancellationToken)?displayProperty=nameWithType> — returns a stream of <xref:System.Diagnostics.ProcessOutputLine> values that distinguish stdout from stderr without string parsing.

#### Fire-and-forget launches

- <xref:System.Diagnostics.Process.StartAndForget*?displayProperty=nameWithType> — starts a child process when you don't intend to wait for it; the runtime detaches the handle automatically.
- <xref:System.Diagnostics.ProcessStartInfo.StartDetached?displayProperty=nameWithType> — detaches from the parent's session/console so the child can outlive a terminal exit.
- <xref:System.Diagnostics.ProcessStartInfo.KillOnParentExit?displayProperty=nameWithType> (Windows only) — the child is terminated when the parent process exits.

#### SafeProcessHandle lifecycle methods

<xref:Microsoft.Win32.SafeHandles.SafeProcessHandle> gains lifecycle methods for advanced scenarios:

- <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.Start(System.Diagnostics.ProcessStartInfo)?displayProperty=nameWithType> and a new <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.ProcessId?displayProperty=nameWithType> property — launch and identify processes without going through <xref:System.Diagnostics.Process> itself.
- <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.Kill?displayProperty=nameWithType> and <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.Signal(System.Runtime.InteropServices.PosixSignal)?displayProperty=nameWithType> — terminate or signal a process by handle.
- <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.WaitForExit?displayProperty=nameWithType> and <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.WaitForExitAsync(System.Threading.CancellationToken)?displayProperty=nameWithType> — wait for a process to exit by handle.

#### Tighter handle control

- <xref:System.Diagnostics.ProcessStartInfo.InheritedHandles?displayProperty=nameWithType> — specify exactly which OS handles a child process inherits, instead of using the all-or-nothing `UseShellExecute = false` default.
- <xref:System.Diagnostics.ProcessStartInfo.StandardInputHandle?displayProperty=nameWithType>, <xref:System.Diagnostics.ProcessStartInfo.StandardOutputHandle?displayProperty=nameWithType>, and <xref:System.Diagnostics.ProcessStartInfo.StandardErrorHandle?displayProperty=nameWithType> — supply already-open <xref:Microsoft.Win32.SafeHandles.SafeFileHandle?displayProperty=nameWithType> values for redirection without the framework opening new ones.

#### Suspended starts and process lookup

`System.Diagnostics.Process` adds finer control over starting and finding processes:

- <xref:System.Diagnostics.ProcessStartInfo.StartSuspended?displayProperty=nameWithType> starts a process in a suspended state on Windows and macOS. Pair it with <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.Resume?displayProperty=nameWithType> to let it run once you've finished any setup, such as attaching a debugger or configuring job objects.
- <xref:System.Diagnostics.Process.TryGetProcessById(System.Int32,System.Diagnostics.Process@)?displayProperty=nameWithType> returns `false` instead of throwing when no process with the given ID exists.
- <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.Open(System.Int32)> and <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle.TryOpen(System.Int32,Microsoft.Win32.SafeHandles.SafeProcessHandle@)> open a handle to an existing process by ID.

```csharp
var startInfo = new ProcessStartInfo("worker.exe") { StartSuspended = true };
using var process = Process.Start(startInfo)!;

// Attach diagnostics or configure job objects while the process is suspended,
// then call Resume to start execution once your setup is complete.
AttachProfiler(process);
process.SafeHandle.Resume();
```

#### Signal processes and inspect termination status

<xref:System.Diagnostics.Process> exposes process signaling and exit-status APIs directly, without requiring you to work through <xref:Microsoft.Win32.SafeHandles.SafeProcessHandle>:

- <xref:System.Diagnostics.Process.Signal(System.Runtime.InteropServices.PosixSignal)?displayProperty=nameWithType> sends a POSIX signal to the process.
- <xref:System.Diagnostics.Process.WaitForExitStatus?displayProperty=nameWithType>, <xref:System.Diagnostics.Process.TryWaitForExitStatus(System.TimeSpan,System.Diagnostics.ProcessExitStatus@)?displayProperty=nameWithType>, and <xref:System.Diagnostics.Process.WaitForExitStatusAsync(System.Threading.CancellationToken)?displayProperty=nameWithType> return a <xref:System.Diagnostics.ProcessExitStatus>, which distinguishes a normal exit from a signal-based termination.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="ProcessSignal":::

### Console FORCE_COLOR support

.NET console output now honors the [`FORCE_COLOR`](https://force-color.org/) standard alongside the existing `NO_COLOR` support. When `FORCE_COLOR` is set, <xref:System.Console.IsOutputRedirected?displayProperty=nameWithType> no longer suppresses ANSI escape codes. This is useful when you pipe `dotnet run` output through `tee`, into a CI log viewer, or through `less -R`:

```bash
FORCE_COLOR=1 dotnet run | tee build.log
```

## In-memory stream adapters

Four new `Stream` types wrap common in-memory data so you can pass them to any API that expects a `Stream`, without copying into a `MemoryStream` first:

- `ReadOnlyMemoryStream` exposes a `ReadOnlyMemory<byte>` as a read-only stream.
- `WritableMemoryStream` exposes a writable `Memory<byte>` as a fixed-size stream.
- `ReadOnlySequenceStream` exposes a `ReadOnlySequence<byte>` (for example, buffers from `System.IO.Pipelines`) as a stream without flattening it.
- `StringStream` reads a `string` or `ReadOnlyMemory<char>` as a stream using a specified encoding.

```csharp
using System.IO;
using System.Text;

// Pass a string to an API that takes a Stream, with no intermediate byte[] needed.
using Stream config = new StringStream(yamlText, Encoding.UTF8);
var settings = ParseConfiguration(config);

// Expose an existing buffer as a read-only stream, for example as an HTTP request body.
ReadOnlyMemory<byte> payload = GetPayload();
using Stream body = new ReadOnlyMemoryStream(payload);
await httpClient.PostAsync(uri, new StreamContent(body));
```

`ReadOnlySequenceStream` is especially useful with `System.IO.Pipelines`, because it streams directly over the segments of a `ReadOnlySequence<byte>` instead of allocating a contiguous copy.

## Text, serialization, and data handling

- [String and character enhancements](#string-and-character-enhancements)
- [Base64 encoding improvements](#base64-encoding-improvements)
- [UTF validation and invalid-subsequence search](#utf-validation-and-invalid-subsequence-search)
- [System.Text.Json improvements](#systemtextjson-improvements)
- [Regular expression improvements](#regular-expression-improvements)
- [Read CBOR incrementally](#read-cbor-incrementally)

### String and character enhancements

.NET 11 introduces significant enhancements to string and character manipulation APIs, making it easier to work with Unicode characters and runes.

#### Rune support in String methods

The <xref:System.String> class now includes methods that accept <xref:System.Text.Rune> parameters, enabling you to search, replace, and manipulate strings using Unicode scalar values directly. These new methods include:

- `Contains` - Check if a string contains a specific rune: <xref:System.String.Contains(System.Text.Rune)?displayProperty=nameWithType> and <xref:System.String.Contains(System.Text.Rune,System.StringComparison)?displayProperty=nameWithType>.
- `StartsWith` and `EndsWith` - Check if a string starts or ends with a specific rune: <xref:System.String.StartsWith(System.Text.Rune)?displayProperty=nameWithType>, <xref:System.String.StartsWith(System.Text.Rune,System.StringComparison)?displayProperty=nameWithType>, <xref:System.String.EndsWith(System.Text.Rune)?displayProperty=nameWithType>, and <xref:System.String.EndsWith(System.Text.Rune,System.StringComparison)?displayProperty=nameWithType>.
- `IndexOf` and `LastIndexOf` - Find the position of a rune in a string: <xref:System.String.IndexOf(System.Text.Rune)?displayProperty=nameWithType>, <xref:System.String.IndexOf(System.Text.Rune,System.StringComparison)?displayProperty=nameWithType>, <xref:System.String.LastIndexOf(System.Text.Rune)?displayProperty=nameWithType>, and <xref:System.String.LastIndexOf(System.Text.Rune,System.StringComparison)?displayProperty=nameWithType>.
- `Replace` - Replace occurrences of one rune with another: <xref:System.String.Replace(System.Text.Rune,System.Text.Rune)?displayProperty=nameWithType>.
- `Split` - Split a string using a rune as the separator: <xref:System.String.Split(System.Text.Rune,System.StringSplitOptions)?displayProperty=nameWithType> and <xref:System.String.Split(System.Text.Rune,System.Int32,System.StringSplitOptions)?displayProperty=nameWithType>.
- `Trim`, `TrimStart`, and `TrimEnd` - Trim runes from strings: <xref:System.String.Trim(System.Text.Rune)?displayProperty=nameWithType>, <xref:System.String.TrimStart(System.Text.Rune)?displayProperty=nameWithType>, and <xref:System.String.TrimEnd(System.Text.Rune)?displayProperty=nameWithType>.

Many of these methods include overloads that accept a <xref:System.StringComparison> parameter for culture-aware comparisons.

#### Char.Equals with StringComparison

The <xref:System.Char> struct now includes an <xref:System.Char.Equals(System.Char,System.StringComparison)?displayProperty=nameWithType> method that accepts a <xref:System.StringComparison> parameter, allowing you to compare characters using culture-aware or ordinal comparisons.

#### Rune support in TextInfo

The <xref:System.Globalization.TextInfo> class now provides <xref:System.Globalization.TextInfo.ToLower(System.Text.Rune)?displayProperty=nameWithType> and <xref:System.Globalization.TextInfo.ToUpper(System.Text.Rune)?displayProperty=nameWithType> methods that accept <xref:System.Text.Rune> parameters, enabling you to perform case conversions on individual Unicode scalar values.

#### StringBuilder.MoveChunks

To transfer content without copying characters, use the new static <xref:System.Text.StringBuilder.MoveChunks(System.Text.StringBuilder)?displayProperty=nameWithType> method, which moves all content from a source `StringBuilder` to a new `StringBuilder`. After the call, the source `StringBuilder` has a length of 0:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="StringBuilderMoveChunks":::

### Base64 encoding improvements

.NET 11 adds new APIs and overloads to the existing <xref:System.Buffers.Text.Base64> type, providing comprehensive support for Base64 encoding and decoding. These additions offer improved performance and flexibility compared to existing methods.

#### New Base64 APIs

The new APIs support encoding and decoding operations with various input and output formats:

- **Encoding to chars**: <xref:System.Buffers.Text.Base64.EncodeToChars*> and <xref:System.Buffers.Text.Base64.EncodeToString*>
- **Encoding to UTF-8**: <xref:System.Buffers.Text.Base64.EncodeToUtf8*>
- **Decoding from chars**: <xref:System.Buffers.Text.Base64.DecodeFromChars*>
- **Decoding from UTF-8**: <xref:System.Buffers.Text.Base64.DecodeFromUtf8*>

These methods provide both high-level convenience methods (that allocate and return arrays or strings) and low-level span-based methods (for zero-allocation scenarios).

### UTF validation and invalid-subsequence search

<xref:System.Text.Unicode?displayProperty=fullName> has two new complementary features. <xref:System.Text.Unicode.Utf16.IsValid(System.ReadOnlySpan{System.Char})?displayProperty=nameWithType> answers whether a sequence is well-formed UTF-16 without scanning twice, and <xref:System.Text.Unicode.Utf8.IndexOfInvalidSubsequence(System.ReadOnlySpan{System.Byte})?displayProperty=nameWithType> / <xref:System.Text.Unicode.Utf16.IndexOfInvalidSubsequence(System.ReadOnlySpan{System.Char})?displayProperty=nameWithType> return the position of the first ill-formed code-unit sequence (or `-1` for valid input). Together, these methods let parsers, validators, and serializers report precise errors instead of generic encoding-error messages.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="UtfValidation":::

### System.Text.Json improvements

#### Union serialization

**C# union types.** Starting in .NET 11, C# offers union types. For example, `Reading` can hold an `int` or a `string`:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonUnionType":::

A `Reading` that holds `"hello"` round-trips as a JSON string:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonUnionSerialization":::

The serializer writes the active case directly, without a wrapper or `$type` discriminator. The JSON output depends on the active case:

| Union state | JSON output |
| - | - |
| `string` case with value `"hello"` | `"hello"` |
| `int` case with value `42` | `42` |
| Default struct union with no active case | `null` |

By default, deserialization selects the case from the JSON token kind: number, string, Boolean, array, or object. Two object cases share a token kind, so the default classifier can't distinguish them. To select an object case by its root-level property names, use <xref:System.Text.Json.Serialization.JsonUnionAttribute> with the built-in <xref:System.Text.Json.Serialization.JsonUnionTypeStructuralClassifier>:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonUnionStructuralType":::

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonUnionStructuralClassifier":::

The payload's `Lives` property selects `Cat`. The structural classifier narrows the candidate cases by root-level property names and required properties; it doesn't inspect property values or nested content. If the payload matches zero or multiple cases, deserialization throws <xref:System.Text.Json.JsonException>. The classifier doesn't support multiple non-object cases with the same JSON token kind, nested unions, polymorphic cases, or <xref:System.Text.Json.Serialization.ReferenceHandler.Preserve?displayProperty=nameWithType>.

For other case-selection rules, derive from <xref:System.Text.Json.Serialization.JsonTypeClassifierFactory>. The factory receives a <xref:System.Text.Json.Serialization.JsonTypeClassifierContext> describing the union cases and creates a <xref:System.Text.Json.Serialization.JsonTypeClassifier> delegate that reads JSON and selects a case type. Register the factory on one union through <xref:System.Text.Json.Serialization.JsonUnionAttribute.TypeClassifier?displayProperty=nameWithType>, on an options instance through <xref:System.Text.Json.JsonSerializerOptions.TypeClassifiers?displayProperty=nameWithType>, or on a source-generated context through <xref:System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute.TypeClassifiers?displayProperty=nameWithType>.

<xref:System.Text.Json.Serialization.JsonNumberHandlingAttribute> is honored on C# unions, and the source generator no longer reports [SYSLIB1227](../../../fundamentals/syslib-diagnostics/syslib1220-1229.md) for unions that use custom converters.

If a union declares separate `T` and `T?` cases, the serializer selects the nullable case for a `null` payload. A non-null payload is ambiguous without a custom classifier because both cases use the same JSON shape. If no case accepts `null`, JSON `null` deserializes to the default union value, which also serializes as `null`.

For language syntax, see [C# 15 union types](../../../csharp/whats-new/csharp-15.md#union-types). For more about classifiers and advanced contract metadata such as <xref:System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Union?displayProperty=nameWithType> and <xref:System.Text.Json.Serialization.Metadata.JsonUnionCaseInfo>, see [Serialize union types](../../../standard/serialization/system-text-json/union-types.md).

**F# discriminated unions.** The serializer represents cases without fields as JSON strings and cases with fields as JSON objects that contain a `$type` discriminator:

```fsharp
type Shape =
    | Point
    | Circle of radius: float
    | Rectangle of height: float * length: float
```

| F# value | JSON output |
| - | - |
| `Point` | `"Point"` |
| `Circle 3.14` | `{"$type":"Circle","radius":3.14}` |
| `Rectangle(10.0, 20.0)` | `{"$type":"Rectangle","height":10,"length":20}` |

For cases without fields, deserialization accepts both `"Point"` and `{"$type":"Point"}`. Class, struct, and recursive unions are supported. <xref:System.Text.Json.JsonSerializerOptions.PropertyNamingPolicy?displayProperty=nameWithType> applies to case and field names, while <xref:System.Text.Json.Serialization.JsonPropertyNameAttribute> on a case takes precedence. You can also change the discriminator property through <xref:System.Text.Json.Serialization.JsonPolymorphicAttribute.TypeDiscriminatorPropertyName?displayProperty=nameWithType>.

> [!IMPORTANT]
> F# discriminated-union support uses reflection only. It doesn't support `System.Text.Json` source generation, trimming, or Native AOT.

#### JSON Lines output

Four <xref:System.Text.Json.JsonSerializer.SerializeAsyncEnumerable*?displayProperty=nameWithType> overloads serialize an <xref:System.Collections.Generic.IAsyncEnumerable`1> to either a <xref:System.IO.Stream> or <xref:System.IO.Pipelines.PipeWriter>. For each output type, one overload accepts <xref:System.Text.Json.JsonSerializerOptions> and one accepts <xref:System.Text.Json.Serialization.Metadata.JsonTypeInfo`1>.

With the default `topLevelValues: false`, the output remains one JSON array. Set `topLevelValues: true` to write canonical JSON Lines (JSONL). The serializer writes each value compactly and follows it with a line feed (LF), `\n`, including the final value. JSONL output uses LF regardless of <xref:System.Text.Json.JsonSerializerOptions.NewLine?displayProperty=nameWithType> and ignores <xref:System.Text.Json.JsonSerializerOptions.WriteIndented?displayProperty=nameWithType> so that each value occupies one line.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonSerializeAsyncEnumerablePipe":::

#### Polymorphic hierarchy support

For a C# `closed` hierarchy, the serializer can infer the derived types, including generic specializations, and assign deterministic discriminators. Configure inference at any of these scopes:

```csharp
[JsonPolymorphic(InferClosedTypePolymorphism = true)]
closed record Shape;
record Circle(double Radius) : Shape;
record Square(double Length) : Shape;
```

- Set <xref:System.Text.Json.JsonSerializerOptions.InferClosedTypePolymorphism?displayProperty=nameWithType> for reflection-based serialization or as a global runtime setting.
- Set <xref:System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute.InferClosedTypePolymorphism?displayProperty=nameWithType> so the source generator creates the required metadata at compile time.
- Set <xref:System.Text.Json.Serialization.JsonPolymorphicAttribute.InferClosedTypePolymorphism?displayProperty=nameWithType> to `true` on one hierarchy. Set it to `false` to opt that hierarchy out of a global setting.

Inference also works through nested closed hierarchies, so a `closed` type that derives from another `closed` type contributes its own derived types to the inferred list. Explicit <xref:System.Text.Json.Serialization.JsonDerivedTypeAttribute> registrations take precedence and replace the inferred list instead of extending it. If source-generated metadata doesn't contain inferred or explicitly registered derived types, enabling only the runtime option fails fast. A per-type opt-in on a type that isn't `closed` throws <xref:System.InvalidOperationException> with reflection and produces a source-generation error. Combining a per-type opt-in with explicit derived-type registrations produces a source-generation warning because the explicit registrations replace inference.

Open generic derived-type registrations are also supported. For example, one attribute registers `Derived<T>` for every compatible closed `Base<T>`:

```csharp
[JsonDerivedType(typeof(Derived<>), "derived")]
public class Base<T> { }
public class Derived<T> : Base<T> { }
```

The resolver supports these type-argument patterns:

| Pattern | Example resolution |
| - | - |
| Direct binding | `Base<int>` resolves `Derived<T> : Base<T>` to `Derived<int>`. |
| Reordered parameters | `Base<int, string>` resolves `Derived<U, V> : Base<V, U>` to `Derived<string, int>`. |
| Partially concrete parameters | `Base<string, int>` resolves `Derived<T> : Base<T, int>` to `Derived<string>`. |
| Nested or array parameters | `Base<List<int>>` resolves `Derived<T> : Base<List<T>>` to `Derived<int>`. |

The same rules apply to generic interfaces and types nested in generic outer types. The registration must resolve to exactly one closed derived type and satisfy its generic constraints. A ground-type mismatch, an unbound parameter, a constraint violation, an ambiguous match, or an incompatible shape throws <xref:System.InvalidOperationException> with reflection. For source generation, an unresolved registration produces the [SYSLIB1229](../../../fundamentals/syslib-diagnostics/syslib1220-1229.md) warning, and the generated hierarchy still fails when the serializer configures it. Suppressing the warning doesn't make the registration valid.

For serializer configuration guidance, see [Infer polymorphism from a closed hierarchy](../../../standard/serialization/system-text-json/polymorphism.md#infer-polymorphism-from-a-closed-hierarchy) and [Configure open generic derived types](../../../standard/serialization/system-text-json/polymorphism.md#configure-open-generic-derived-types). For more information about the language feature, see [C# 15 closed hierarchies](../../../csharp/whats-new/csharp-15.md#closed-hierarchies).

#### Source-generation and contract metadata

The `System.Text.Json` source generator supports more contract shapes, and the generated unsafe accessors are compatible with the updated C# unsafe semantics:

- Members marked with <xref:System.Text.Json.Serialization.JsonIncludeAttribute> can have `private`, `internal`, or `protected` accessors. Private, internal, and protected fields are also supported when they're marked with `[JsonInclude]`.
- An inaccessible constructor marked with <xref:System.Text.Json.Serialization.JsonConstructorAttribute> can participate in deserialization.
- An omitted `init`-only property retains its property-initializer value. The generated contract sets the property after construction only when the JSON payload contains that property.

`JsonSerializer` also deserializes types with constructors whose parameters use `in`, `ref`, `out`, or `ref readonly`. The serializer binds `in`, `ref`, and `ref readonly` parameters by their element type. An `out` parameter doesn't bind a JSON constructor argument; the constructor receives an initialized output location, and a matching writable property can receive the JSON value after construction.

For more information, see [Source-generation modes in System.Text.Json](../../../standard/serialization/system-text-json/source-generation-modes.md) and [Use immutable types and properties](../../../standard/serialization/system-text-json/immutability.md).

Generic <xref:System.Text.Json.JsonSerializerOptions.GetTypeInfo``1?displayProperty=nameWithType> and <xref:System.Text.Json.JsonSerializerOptions.TryGetTypeInfo``1(System.Text.Json.Serialization.Metadata.JsonTypeInfo{``0}@)?displayProperty=nameWithType> methods return <xref:System.Text.Json.Serialization.Metadata.JsonTypeInfo`1> without a manual cast. `GetTypeInfo<T>()` throws if the options can't resolve the type, while `TryGetTypeInfo<T>()` returns `false` in that case. Resolver and configuration exceptions can still propagate. These methods simplify metadata access in source-generation, Native AOT, and polymorphic serialization scenarios. For more information, see [Get strongly typed metadata](../../../standard/serialization/system-text-json/custom-contracts.md#get-strongly-typed-metadata).

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonTypeInfoGeneric":::

#### Property names and ignore conditions

<xref:System.Text.Json.JsonNamingPolicy.PascalCase?displayProperty=nameWithType> and <xref:System.Text.Json.Serialization.JsonKnownNamingPolicy.PascalCase?displayProperty=nameWithType> provide built-in PascalCase conversion. Apply <xref:System.Text.Json.Serialization.JsonNamingPolicyAttribute> to a class, struct, interface, property, or field to select a naming policy at the type or member level.

Naming precedence, from highest to lowest, is <xref:System.Text.Json.Serialization.JsonPropertyNameAttribute>, a member-level `[JsonNamingPolicy]`, a type-level `[JsonNamingPolicy]`, <xref:System.Text.Json.JsonSerializerOptions.PropertyNamingPolicy?displayProperty=nameWithType>, and the original member name.

<xref:System.Text.Json.Serialization.JsonIgnoreAttribute> can now set a default ignore condition on classes, structs, and interfaces. A member-level `[JsonIgnore]` takes precedence over the type-level condition, which takes precedence over <xref:System.Text.Json.JsonSerializerOptions.DefaultIgnoreCondition?displayProperty=nameWithType>. <xref:System.Text.Json.Serialization.JsonIgnoreCondition.Always?displayProperty=nameWithType> isn't valid at the type level; reflection throws <xref:System.InvalidOperationException>, while source generation reports `SYSLIB1226` and ignores the type-level attribute.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="JsonNamingIgnore":::

For more information, see [Apply a naming policy to a type or member](../../../standard/serialization/system-text-json/customize-properties.md#apply-a-naming-policy-to-a-type-or-member) and [Ignore properties based on a type-level condition](../../../standard/serialization/system-text-json/ignore-properties.md#ignore-properties-based-on-a-type-level-condition).

#### Converters and collection contracts

**Numeric converters.** The serializer includes built-in converters for <xref:System.Numerics.BFloat16>, <xref:System.Numerics.Decimal32>, <xref:System.Numerics.Decimal64>, and <xref:System.Numerics.Decimal128>. The converters support number-handling options, dictionary keys, and JSON Schema export. For generated metadata, <xref:System.Text.Json.Serialization.Metadata.JsonMetadataServices> exposes <xref:System.Text.Json.Serialization.Metadata.JsonMetadataServices.BFloat16Converter?displayProperty=nameWithType>, <xref:System.Text.Json.Serialization.Metadata.JsonMetadataServices.Decimal32Converter?displayProperty=nameWithType>, <xref:System.Text.Json.Serialization.Metadata.JsonMetadataServices.Decimal64Converter?displayProperty=nameWithType>, and <xref:System.Text.Json.Serialization.Metadata.JsonMetadataServices.Decimal128Converter?displayProperty=nameWithType>.

**Open generic converters.** <xref:System.Text.Json.Serialization.JsonConverterAttribute> can reference an open generic converter on a generic type or member:

```csharp
[JsonConverter(typeof(OptionConverter<>))]
public readonly struct Option<T> { }
```

The converter's total generic arity must match the target type's arity. The serializer closes the converter with the target type arguments without requiring <xref:System.Text.Json.Serialization.JsonConverterFactory>. For supported nesting patterns, constraints, and error behavior, see [Use open generic converters with `JsonConverter`](../../../standard/serialization/system-text-json/converters-how-to.md#use-open-generic-converters-with-jsonconverter).

**Extension data.** A member marked with <xref:System.Text.Json.Serialization.JsonExtensionDataAttribute> can use <xref:System.Collections.Generic.IReadOnlyDictionary`2> with `string` keys and either `object` or <xref:System.Text.Json.JsonElement> values. During deserialization, the serializer assigns a mutable <xref:System.Collections.Generic.Dictionary`2> implementation. If the property already contains entries, the serializer copies them first and then adds JSON properties, so an incoming value replaces an existing value with the same key. A <xref:System.Text.Json.Nodes.JsonObject> extension-data member writes its child properties directly into the containing object. For example, an `Id` property and a `JsonObject` entry named `nested` produce `{"Id":1,"nested":true}`. For more information, see [Handle overflow JSON](../../../standard/serialization/system-text-json/handle-overflow.md#handle-overflow-json).

**Read-only sets.** <xref:System.Text.Json.JsonSerializer> serializes <xref:System.Collections.Generic.IReadOnlySet`1> as a JSON array and deserializes it into a <xref:System.Collections.Generic.HashSet`1> instance. For source-generated metadata, <xref:System.Text.Json.Serialization.Metadata.JsonMetadataServices.CreateIReadOnlySetInfo*?displayProperty=nameWithType> creates the collection contract.

For more information about built-in numeric and collection contracts, see [Supported types in System.Text.Json](../../../standard/serialization/system-text-json/supported-types.md).

#### Writer reuse

<xref:System.Text.Json.Utf8JsonWriter.Reset*> accepts a <xref:System.Text.Json.JsonWriterOptions> together with either a <xref:System.IO.Stream> or <xref:System.Buffers.IBufferWriter`1>. After you flush one JSON document, you can reuse or pool the writer with a different output target and different options instead of constructing another writer instance:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="Utf8JsonWriterReset":::

For more information, see [Reuse a writer](../../../standard/serialization/system-text-json/use-utf8jsonwriter.md#reuse-a-writer).

You can also opt a single closed hierarchy in to this behavior with <xref:System.Text.Json.Serialization.JsonPolymorphicAttribute.InferClosedTypePolymorphism?displayProperty=nameWithType>, without changing the application-wide `JsonSerializerOptions` setting:

```csharp
[JsonPolymorphic(InferClosedTypePolymorphism = true)]
public abstract record Shape;

public sealed record Circle(double Radius) : Shape;

public sealed record Square(double Side) : Shape;
```

### Regular expression improvements

#### AnyNewLine option

A new <xref:System.Text.RegularExpressions.RegexOptions.AnyNewLine?displayProperty=nameWithType> flag makes `^`, `$`, and `.` treat the full set of Unicode newline characters as line terminators—not just `\n`. This helps when parsing text that mixes Windows (`\r\n`), Unix (`\n`), and Unicode-specific (`\u0085`, `\u2028`, `\u2029`) line endings.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="RegexAnyNewLine":::

#### Regex engine and source generator fixes

.NET 11 includes several regex correctness and code-quality fixes:

- The non-backtracking engine no longer takes super-linear time on certain nested-loop patterns and produces correct results for cases that previously diverged.
- The regex compiler and source generator handle `resumeAt` correctly when a conditional appears inside a loop body.
- The [SYSLIB1045](../../../fundamentals/syslib-diagnostics/syslib1040-1049.md) code fixer no longer creates duplicate class names when applied across multiple partial declarations of the same class.

### Read CBOR incrementally

<xref:System.Formats.Cbor.CborReader> can read a Concise Binary Object Representation (CBOR) payload that's delivered in chunks, without first copying the entire document into contiguous memory. To read incrementally:

1. Construct a reader with `isFinalBlock: false`.
1. Check for `CborReaderState.NeedsMoreData` to determine when the reader needs the next chunk.
1. Pass the next chunk to `CborReader.SlideData`, and set `isFinalBlock` to `true` for the last chunk.

Incremental reading requires <xref:System.Formats.Cbor.CborConformanceMode.Lax?displayProperty=nameWithType>. Other conformance modes reject a non-final block.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="CborIncrementalRead":::

## Compression and archive formats

- [Compression enhancements](#compression-enhancements)
- [Zstandard compression](#zstandard-compression)
- [Tar archive format selection](#tar-archive-format-selection)

### Compression enhancements

.NET 11 includes several improvements to compression APIs.

#### ZIP archive entry access modes

The <xref:System.IO.Compression.ZipArchiveEntry> class now supports opening entries with specific file access modes through new overloads: <xref:System.IO.Compression.ZipArchiveEntry.Open(System.IO.FileAccess)?displayProperty=nameWithType> and <xref:System.IO.Compression.ZipArchiveEntry.OpenAsync(System.IO.FileAccess,System.Threading.CancellationToken)?displayProperty=nameWithType>. These overloads accept a <xref:System.IO.FileAccess> parameter and allow you to open ZIP entries for read, write, or read-write access.

Additionally, a new <xref:System.IO.Compression.ZipArchiveEntry.CompressionMethod> property exposes the compression method used for an entry through the <xref:System.IO.Compression.ZipCompressionMethod> enum, which includes values for <xref:System.IO.Compression.ZipCompressionMethod.Stored>, <xref:System.IO.Compression.ZipCompressionMethod.Deflate>, and <xref:System.IO.Compression.ZipCompressionMethod.Deflate64>.

#### ZIP CRC32 validation

<xref:System.IO.Compression.ZipArchive> validates the CRC32 checksum when reading ZIP entries. Corrupted or truncated archives that previously passed without error now throw <xref:System.IO.InvalidDataException>, helping you detect data integrity issues early.

#### ZIP archive password support

<xref:System.IO.Compression.ZipArchiveEntry> now supports password-protected entries for read and write operations. Use the new password-aware <xref:System.IO.Compression.ZipArchiveEntry.Open(System.ReadOnlySpan{System.Char})?displayProperty=nameWithType> and <xref:System.IO.Compression.ZipArchiveEntry.OpenAsync(System.ReadOnlySpan{System.Char},System.Threading.CancellationToken)?displayProperty=nameWithType> overloads, and use password plus <xref:System.IO.Compression.ZipEncryptionMethod> when you create encrypted entries.

#### DeflateStream and GZipStream behavior change

Starting in .NET 11, <xref:System.IO.Compression.DeflateStream> and <xref:System.IO.Compression.GZipStream> always write format headers and footers to the output stream, even when no data is written. This ensures the output is a valid compressed stream according to the Deflate and GZip specifications.

Previously, these streams didn't produce any output if no data was written, resulting in an empty output stream. This change ensures compatibility with tools that expect properly formatted compressed streams.

For more information, see [DeflateStream and GZipStream write headers and footers for empty payload](../../compatibility/core-libraries/11/deflatestream-gzipstream-empty-payload.md).

#### Span-based Deflate, ZLib, and GZip APIs

<xref:System.IO.Compression> now offers `Span<byte>`/`ReadOnlySpan<byte>` encode and decode entry points for the Deflate, ZLib, and GZip formats. The new APIs, on types such as <xref:System.IO.Compression.DeflateEncoder>, <xref:System.IO.Compression.ZLibEncoder>, and <xref:System.IO.Compression.GZipEncoder>, mirror the shape of <xref:System.IO.Compression.BrotliEncoder>/<xref:System.IO.Compression.BrotliDecoder> and the Zstandard primitives. You can compress and decompress buffers without allocating a `Stream`. This is useful for high-throughput scenarios such as protocol parsers, log shippers, and middleware that already operate on spans.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="ZLibEncoderSpan":::

The streamless `DeflateEncoder`, `DeflateDecoder`, `ZLibEncoder`, `ZLibDecoder`, `GZipEncoder`, and `GZipDecoder` types also gain a `Reset()` method. Resetting returns an instance to its initial state so you can process another independent payload without allocating a replacement encoder or decoder.

### Zstandard compression

The Zstandard compression APIs, for example, <xref:System.IO.Compression.ZstandardStream> and <xref:System.IO.Compression.ZstandardEncoder>, are now part of the <xref:System.IO.Compression> namespace, alongside `DeflateStream`, `GZipStream`, and `BrotliStream`. The API surface is otherwise unchanged.

### Tar archive format selection

New overloads on <xref:System.Formats.Tar.TarFile.CreateFromDirectory*> and <xref:System.Formats.Tar.TarFile.CreateFromDirectoryAsync*> accept a <xref:System.Formats.Tar.TarEntryFormat> parameter, giving you direct control over the archive format. Previously, `CreateFromDirectory` always produced Pax archives. The new overloads support all four tar formats—Pax, Ustar, GNU, and V7—for compatibility with specific tools and environments.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="TarArchiveFormat":::

`TarReader` can now also read entries that use the GNU sparse format 1.0 (PAX) representation. The earlier 0.1 representation was already supported. With 1.0 support in place, `TarReader` matches what modern `tar` implementations write by default for sparse files.

## Collections, numerics, and low-level I/O

- [LINQ join improvements](#linq-join-improvements)
- [IEEE 754 decimal floating-point types](#ieee-754-decimal-floating-point-types)
- [Correctly rounded decimal and BigInteger conversions](#correctly-rounded-decimal-and-biginteger-conversions)
- [Partial numeric parsing](#partial-numeric-parsing)
- [Generic Complex\<T>](#generic-complext)
- [BFloat16 support in BitConverter](#bfloat16-support-in-bitconverter)
- [Floating-point hex formatting and parsing](#floating-point-hex-formatting-and-parsing)
- [Numerics improvements](#numerics-improvements)
- [Random generic methods](#random-generic-methods)
- [Cross-lane vector operations](#cross-lane-vector-operations)
- [Low-level I/O improvements](#low-level-io-improvements)
- [Collections improvements](#collections-improvements)

### LINQ join improvements

LINQ adds join enhancements, including a new `FullJoin` operation and tuple-returning overloads for the existing `Join` and `GroupJoin` methods. These APIs are available on <xref:System.Linq.Enumerable>, <xref:System.Linq.Queryable>, and <xref:System.Linq.AsyncEnumerable>.

#### LeftJoin, RightJoin, and FullJoin

Because the three outer-join operations return tuples directly, you can work with them easily using pattern matching and deconstruction:

- <xref:System.Linq.Enumerable.LeftJoin*?displayProperty=nameWithType> — Returns all elements from the left (outer) sequence. When there's no matching element in the right (inner) sequence, the inner element is `default` (typically `null` for reference types).
- <xref:System.Linq.Enumerable.RightJoin*?displayProperty=nameWithType> — Returns all elements from the right (inner) sequence. When there's no matching element in the left (outer) sequence, the outer element is `default` (typically `null` for reference types).
- <xref:System.Linq.Enumerable.FullJoin*?displayProperty=nameWithType> — Returns all elements from both sequences, using `default` on either side when there's no match.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="LinqJoins":::

### IEEE 754 decimal floating-point types

<xref:System.Numerics> adds <xref:System.Numerics.Decimal32>, <xref:System.Numerics.Decimal64>, and <xref:System.Numerics.Decimal128>, which implement IEEE 754-2019 decimal floating-point semantics. These types support generic math, infinities, and NaN values, and can help when you need IEEE decimal behavior instead of <xref:System.Decimal>.

The transcendental functions on these types are more accurate. The most significant fix is for `Decimal128` values near a cancellation point, such as `Asin` and `Acos` near ±1 and logarithms near 1, where earlier builds could lose most or all significant digits. For example, `Decimal128.Acos(1 - 1e-30)` previously had an error of roughly 10²⁴ units in the last place (ULPs). It's now well under one ULP.

The change also improves the following behaviors:

- Inverse trigonometric and hyperbolic functions, logarithms, `Pow`, and `RootN` preserve decimal precision during argument reduction.
- `SinPi`, `CosPi`, `TanPi`, and `SinCosPi` return exact results, such as zero for integer inputs written with alternate decimal representations (for example, `SinPi(1000.000)`).
- `Log(x, base)` rounds only once for all three types.
- `Exp2`, `Exp2M1`, and subnormal-result rounding are more accurate.
- Alternate representations of `1`, such as `Acosh(1.0000000)`, no longer return NaN for `Decimal64` and `Decimal128`.

No public APIs were added. Because the rounded results intentionally change, code that compares these results against stored values might observe different last digits. The functions still aren't guaranteed to be correctly rounded.

### Correctly rounded decimal and BigInteger conversions

Conversions between <xref:System.Decimal> and binary floating-point types, and conversions from <xref:System.Numerics.BigInteger> to binary floating-point types, now round the exact source value once to the nearest representable destination value. The previous conversions could lose significant digits or round through an intermediate value, so binaries rebuilt with a .NET 11 SDK can produce different results than they did on earlier versions. There's no compatibility switch to restore the previous conversion algorithms.

For example, converting the `double` literal `1.23` to `decimal` now preserves the exact binary floating-point value rather than producing `1.23`. If a value is meant to be decimal, use a decimal literal such as `1.23m` instead of converting a `double` literal. For complete guidance, including previous and new behavior for each conversion direction, see [Decimal and BigInteger floating-point conversions are correctly rounded](../../compatibility/core-libraries/11/decimal-biginteger-floating-point-conversions.md).

### Partial numeric parsing

<xref:System.Numerics.INumberBase`1.TryParsePartial*?displayProperty=nameWithType> adds partial parsing overloads that report consumed input. Use this API to parse delimited formats, such as CSV, so you don't copy substrings.

### Generic Complex\<T>

<xref:System.Numerics.Complex`1> is now available, so complex arithmetic can use several floating-point types, including `float`, `Half`, `BFloat16`, and the new decimal floating-point types.

#### Tuple-returning Join and GroupJoin overloads

New overloads of <xref:System.Linq.Enumerable.Join*?displayProperty=nameWithType> and <xref:System.Linq.Enumerable.GroupJoin*?displayProperty=nameWithType> return tuples directly without requiring a result selector, and accept an optional <xref:System.Collections.Generic.IEqualityComparer`1> parameter. The optional comparer overload is also available on the `FullJoin`, `LeftJoin`, and `RightJoin` operations.

```csharp
var products = new[] { (Id: 1, Name: "Laptop"), (Id: 2, Name: "Mouse") };
var orders = new[] { (ProductId: 1, Qty: 3), (ProductId: 1, Qty: 1) };

var result = products.Join(orders, p => p.Id, o => o.ProductId);
foreach (var (product, order) in result)
    Console.WriteLine($"{product.Name}: qty={order.Qty}");
// Laptop: qty=3
// Laptop: qty=1
```

### BFloat16 support in BitConverter

The <xref:System.BitConverter> class now includes methods for converting between <xref:System.Numerics.BFloat16> values and byte arrays or bit representations. These new methods include:

- <xref:System.BitConverter.GetBytes(System.Numerics.BFloat16)?displayProperty=nameWithType> - Convert a BFloat16 value to a byte array.
- <xref:System.BitConverter.ToBFloat16(System.Byte[],System.Int32)?displayProperty=nameWithType> and <xref:System.BitConverter.ToBFloat16(System.ReadOnlySpan{System.Byte})?displayProperty=nameWithType> - Convert a byte array to a BFloat16 value.
- <xref:System.BitConverter.BFloat16ToInt16Bits(System.Numerics.BFloat16)?displayProperty=nameWithType>, <xref:System.BitConverter.BFloat16ToUInt16Bits(System.Numerics.BFloat16)?displayProperty=nameWithType>, <xref:System.BitConverter.Int16BitsToBFloat16(System.Int16)?displayProperty=nameWithType>, and <xref:System.BitConverter.UInt16BitsToBFloat16(System.UInt16)?displayProperty=nameWithType> - Methods for converting between BFloat16 and its bit representation as `short` or `ushort`.

BFloat16 (Brain Floating Point) is a 16-bit floating-point format that's commonly used in machine learning and scientific computing.

### Floating-point hex formatting and parsing

`double`, `float`, and `Half` can now be formatted and parsed in their hexadecimal IEEE-754 form. The hex form preserves every bit of the underlying value, making it the right choice for golden-file tests, cross-language interop with C/C++ `printf("%a", ...)`, and any scenario where round-tripping a `double` through decimal text is too lossy.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="FloatingPointHex":::

### Numerics improvements

<xref:System.Numerics.Matrix4x4.GetDeterminant?displayProperty=nameWithType> now uses an SSE-vectorized implementation, improving performance by approximately 15%.

### Random generic methods

<xref:System.Random> gains two new generic methods that work with any numeric type implementing the appropriate generic math interfaces:

- <xref:System.Random.NextInteger*?displayProperty=nameWithType> — Generates a random integer of type `T` where `T` implements `IBinaryInteger<T>` and `IMinMaxValue<T>`. Overloads accept an upper bound or both lower and upper bounds.
- <xref:System.Random.NextBinaryFloat*?displayProperty=nameWithType> — Generates a random floating-point value of type `T` where `T` implements `IBinaryFloatingPointIeee754<T>`.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="RandomGeneric":::

### Cross-lane vector operations

`Vector64<T>`, `Vector128<T>`, `Vector256<T>`, `Vector512<T>`, and `Vector<T>` gain a set of lane construction and composition methods that previously required hand-written shuffles. The new methods fall into a few families:

- **Patterned construction:** `CreateGeometricSequence`, `CreateAlternatingSequence`, and `CreateHarmonicSequence` build a vector from a starting value and a rule.
- **Interleave and de-interleave:** `Zip`, `ZipLower`/`ZipUpper`, `Unzip`, `UnzipEven`/`UnzipOdd`.
- **Rearrange:** The `Concat` family (`ConcatLowerLower`, `ConcatLowerUpper`, `ConcatUpperLower`, `ConcatUpperUpper`) and `Reverse`.

```csharp
using System.Runtime.Intrinsics;

// {1, 2, 4, 8} — each lane is the previous lane times two
Vector128<int> powers = Vector128.CreateGeometricSequence(1, 2);

// Interleave two vectors lane-by-lane
(Vector128<int> lower, Vector128<int> upper) =
    Vector128.Zip(Vector128.Create(1), Vector128.Create(2));
```

These building blocks are useful for image processing, audio digital signal processing (DSP), and other SIMD-intensive workloads that need fine-grained control over vector element layout, without falling back to platform-specific intrinsics.

### Low-level I/O improvements

- [SafeFileHandle pipe support](#safefilehandle-pipe-support)
- [RandomAccess pipe support](#randomaccess-pipe-support)
- [File locking on iOS and tvOS](#file-locking-on-ios-and-tvos)

#### SafeFileHandle pipe support

<xref:Microsoft.Win32.SafeHandles.SafeFileHandle> gains two new members:

- **<xref:Microsoft.Win32.SafeHandles.SafeFileHandle.Type?displayProperty=nameWithType> property:** Reports whether a handle represents a file, pipe, socket, directory, or other OS object, without requiring platform-specific code.
- **<xref:Microsoft.Win32.SafeHandles.SafeFileHandle.CreateAnonymousPipe(Microsoft.Win32.SafeHandles.SafeFileHandle@,Microsoft.Win32.SafeHandles.SafeFileHandle@,System.Boolean,System.Boolean)?displayProperty=nameWithType> method:** Creates a pair of connected anonymous pipe handles with independent async behavior for each end.

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="SafeFileHandlePipe":::

#### RandomAccess pipe support

<xref:System.IO.RandomAccess.Read*?displayProperty=nameWithType> and <xref:System.IO.RandomAccess.Write*?displayProperty=nameWithType> now work with non-seekable handles such as pipes, in addition to regular file handles.

On Windows, `Process` now uses overlapped I/O for redirected stdout/stderr, which reduces thread-pool blocking in process-heavy applications.

#### File locking on iOS and tvOS

On iOS and tvOS, <xref:System.IO.FileStream> no longer acquires advisory file locks by default. This change avoids operating-system termination when an app is suspended while it holds a file lock. As a result, <xref:System.IO.FileShare> restrictions aren't enforced on those platforms, including between handles in the same process. Mac Catalyst and other platforms keep their existing defaults. To restore locking, set the `System.IO.DisableFileLocking` runtime configuration property to `false`, or set the `DOTNET_SYSTEM_IO_DISABLEFILELOCKING` environment variable to `0`. For more information, see [File locking on iOS and tvOS](runtime.md#file-locking-on-ios-and-tvos).

### Collections improvements

- [BitArray span constructors](#bitarray-span-constructors)
- [BitArray.PopCount](#bitarraypopcount)
- [EqualityComparer\<T>.Create](#equalitycomparertcreate)
- [ConcurrentQueue reliability fix](#concurrentqueue-reliability-fix)

#### BitArray span constructors

<xref:System.Collections.BitArray> now has constructors that accept <xref:System.ReadOnlySpan`1>: `ReadOnlySpan<bool>`, `ReadOnlySpan<byte>`, and `ReadOnlySpan<int>`. You can construct a bit array directly from a slice or stack-allocated data without first allocating an array:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="BitArraySpanConstructor":::

The span-based constructors avoid the intermediate array allocation that `new BitArray(span.ToArray())` requires.

#### BitArray.PopCount

The <xref:System.Collections.BitArray> class now includes a <xref:System.Collections.BitArray.PopCount?displayProperty=nameWithType> method that returns the number of bits set to `true` in the array. This provides an efficient way to count set bits without manually iterating through the array.

#### EqualityComparer\<T>.Create

To create an equality comparer from a key selector function, use the new <xref:System.Collections.Generic.EqualityComparer`1.Create*?displayProperty=nameWithType> factory method. You can pass an optional <xref:System.Collections.Generic.IEqualityComparer`1> for the key type itself:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="EqualityComparerCreate":::

#### ConcurrentQueue reliability fix

<xref:System.Collections.Concurrent.ConcurrentQueue`1.TryDequeue*?displayProperty=nameWithType> and <xref:System.Collections.Concurrent.ConcurrentQueue`1.TryPeek*?displayProperty=nameWithType> no longer spuriously report an empty queue while the queue freezes a segment during growth.

## Extensions and developer platform

- [Discriminated-union scaffolding](#discriminated-union-scaffolding)
- [MetadataLoadContext additions](#metadataloadcontext-additions)
- [Reflection improvements](#reflection-improvements)
- [Assembly.Location override for AssemblyLoadContext](#assemblylocation-override-for-assemblyloadcontext)
- [Reduced contention in System.IO.Pipelines](#reduced-contention-in-systemiopipelines)
- [URI data scheme constant](#uri-data-scheme-constant)
- [StringSyntax attribute enhancements](#stringsyntax-attribute-enhancements)

### Discriminated-union scaffolding

> [!NOTE]
> This is a preview feature in .NET 11.

.NET 11 introduces <xref:System.Runtime.CompilerServices.UnionAttribute?displayProperty=nameWithType> and <xref:System.Runtime.CompilerServices.IUnion?displayProperty=nameWithType> in <xref:System.Runtime.CompilerServices?displayProperty=fullName>. These types are the runtime side of the C# discriminated-union design. They aren't directly user-facing yet—the C# compiler and source generators are the expected producers—but they ship in the framework so libraries can author against the surface now.

For the language-side design, see the [discriminated unions working group overview](https://github.com/dotnet/csharplang/blob/main/meetings/working-groups/discriminated-unions/union-proposals-overview.md).

### MetadataLoadContext additions

<xref:System.Reflection.MetadataLoadContext.GetLoadContext(System.Reflection.Assembly)?displayProperty=nameWithType> returns the load context that produced a given `Assembly`, mirroring the long-existing API on <xref:System.Runtime.Loader.AssemblyLoadContext>. This closes a gap for tooling that reflects over assemblies in an isolated `MetadataLoadContext` and needs to walk back from an `Assembly` reference to the context that owns it:

```csharp
using System.Reflection;
using System.Reflection.Metadata;

string[] paths = [typeof(object).Assembly.Location];
using var mlc = new MetadataLoadContext(new PathAssemblyResolver(paths));
Assembly asm = mlc.LoadFromAssemblyPath(typeof(object).Assembly.Location);

MetadataLoadContext owner = MetadataLoadContext.GetLoadContext(asm)!;
Console.WriteLine(ReferenceEquals(owner, mlc)); // true
```

### URI data scheme constant

A new <xref:System.Uri.UriSchemeData?displayProperty=nameWithType> constant has been added, representing the `data:` URI scheme. This constant provides a standardized way to reference data URIs.

### Reflection improvements

#### Type.GetNullableUnderlyingType

<xref:System.Type?displayProperty=fullName> gains a new virtual method, <xref:System.Type.GetNullableUnderlyingType?displayProperty=nameWithType>, that returns the underlying value type for a `Nullable<T>` type, or `null` for any non-nullable type. Implementations are provided on <xref:System.Type>, <xref:System.Reflection.Emit.TypeBuilder>, <xref:System.Reflection.Emit.EnumBuilder>, <xref:System.Reflection.Emit.GenericTypeParameterBuilder>, and <xref:System.Reflection.TypeDelegator>:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="NullableUnderlyingType":::

#### ConstructorInfo.GetGenericArguments

<xref:System.Reflection.ConstructorInfo.GetGenericArguments?displayProperty=nameWithType> now has an override, providing a consistent way to retrieve generic type arguments for constructor definitions, matching the behavior already available on other `MethodBase` subclasses.

### Assembly.Location override for AssemblyLoadContext

<xref:System.Runtime.Loader.AssemblyLoadContext.SetAssemblyLocationOverride(System.Func{System.Reflection.Assembly,System.String,System.String})?displayProperty=nameWithType> is a set-once callback that can override the location reported by <xref:System.Reflection.Assembly.Location?displayProperty=nameWithType>. This API helps hosts that stage assemblies in temporary paths or virtualized locations report stable, meaningful locations to diagnostics and resource-loading code.

### Reduced contention in System.IO.Pipelines

<xref:System.IO.Pipelines?displayProperty=fullName> reduces reader and writer lock contention under high fan-in workloads. Internal changes include lower lock scope, local queue continuation scheduling, and pool updates that improve cache locality.

### StringSyntax attribute enhancements

The <xref:System.Diagnostics.CodeAnalysis.StringSyntaxAttribute> class now includes constants for common programming languages:

- <xref:System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.CSharp> - Indicates C# syntax.
- <xref:System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.FSharp> - Indicates F# syntax.
- <xref:System.Diagnostics.CodeAnalysis.StringSyntaxAttribute.VisualBasic> - Indicates Visual Basic syntax.

These constants can be used with the `StringSyntax` attribute to provide better tooling support for string literals containing code in these languages.

## Caching, configuration, and logging

- [Configuration binding](#configuration-binding)
- [MemoryCache OpenTelemetry metrics](#memorycache-opentelemetry-metrics)
- [Options builder validation improvements](#options-builder-validation-improvements)
- [Asynchronous ChangeToken.OnChange](#asynchronous-changetokenonchange)
- [Options-aware HybridCache factories](#options-aware-hybridcache-factories)
- [Asynchronous validation with DataAnnotations](#asynchronous-validation-with-dataannotations)
- [Format DataAnnotations messages](#format-dataannotations-messages)
- [Activity tracing configuration](#activity-tracing-configuration)
- [Generic source-generated logging methods](#generic-source-generated-logging-methods)
- [Improved reliability for configuration file change detection](#improved-reliability-for-configuration-file-change-detection)

### Configuration binding

<xref:Microsoft.Extensions.Configuration?displayProperty=fullName> adds <xref:Microsoft.Extensions.Configuration.ConfigurationIgnoreAttribute?displayProperty=nameWithType>, so models can opt individual properties out of binding declaratively without relying on `BindNonPublicProperties` toggles or custom converters:

```csharp
public sealed class AppOptions
{
    public string Endpoint { get; set; } = "";

    [ConfigurationIgnore]
    public string ComputedKey => Endpoint + ":default";
}
```

`ConfigurationBinder` now also binds an empty array to a constructor parameter instead of throwing.

`PhysicalFilesWatcher` no longer throws when its root directory doesn't yet exist, and `InMemoryDirectoryInfo` resolves `..` and other relative segments consistently with the physical provider.

### MemoryCache OpenTelemetry metrics

<xref:Microsoft.Extensions.Caching.Memory.MemoryCache> now emits a built-in set of OpenTelemetry (OTel)-compatible metrics without an extra adapter package. To opt in, set <xref:Microsoft.Extensions.Caching.Memory.MemoryCacheOptions.TrackStatistics?displayProperty=nameWithType> to `true`:

```csharp
var cache = new MemoryCache(new MemoryCacheOptions
{
    TrackStatistics = true
});
```

The new `Microsoft.Extensions.Caching.Memory.MemoryCache` meter publishes four observable instruments:

- `dotnet.cache.requests` (with a `dotnet.cache.request.result` tag that distinguishes `hit` from `miss`)
- `dotnet.cache.evictions`
- `dotnet.cache.entries`
- `dotnet.cache.estimated_size` (with unit `1`, because its values represent application-defined cache size units rather than bytes)

If you filter telemetry on the hit/miss tag, use `dotnet.cache.request.result`. Earlier builds named the tag `dotnet.cache.request.type`.

Pass an <xref:System.Diagnostics.Metrics.IMeterFactory?displayProperty=nameWithType> to the new <xref:Microsoft.Extensions.Caching.Memory.MemoryCache.%23ctor(Microsoft.Extensions.Options.IOptions{Microsoft.Extensions.Caching.Memory.MemoryCacheOptions},Microsoft.Extensions.Logging.ILoggerFactory,System.Diagnostics.Metrics.IMeterFactory)?displayProperty=nameWithType> constructor overload for per-instance metrics. Without one, the instruments are aggregated process-wide on a shared meter.

### Options builder validation improvements

<xref:Microsoft.Extensions.Options.OptionsBuilder`1> gains a new generic <xref:Microsoft.Extensions.Options.OptionsBuilder`1.Validate*?displayProperty=nameWithType> overload that accepts a type parameter instead of a factory delegate. The type must implement <xref:Microsoft.Extensions.Options.IValidateOptions`1> and be registered in the dependency-injection container. This aligns options validation with the standard DI pattern:

```csharp
services.AddSingleton<IValidateOptions<MyOptions>, MyOptionsValidator>();
services.AddOptions<MyOptions>()
    .Bind(configuration.GetSection("MyOptions"))
    .Validate<MyOptionsValidator>();
```

### Asynchronous ChangeToken.OnChange

<xref:Microsoft.Extensions.Primitives.ChangeToken.OnChange*> now has `Func<Task>` overloads, so you can run asynchronous callbacks without using `async void`. Re-registration now happens after the returned task completes.

### Options-aware HybridCache factories

<xref:Microsoft.Extensions.Caching.Hybrid.HybridCache.GetOrCreateAsync*> now has overloads that provide a mutable factory context. You can set entry-level options, such as expiration and local cache size, from the value-producing callback.

### Asynchronous validation with DataAnnotations

`System.ComponentModel.DataAnnotations` now supports asynchronous validation. A validation rule that needs to do I/O—a database lookup or a remote API call—can now run without blocking a thread. There are three new ways to express an async rule:

- Derive from `AsyncValidationAttribute` and override its `IsValidAsync` method.
- Implement `IAsyncValidatableObject` on the model.
- Call the new `Validator.ValidateObjectAsync`, `TryValidateObjectAsync`, `ValidatePropertyAsync`, and `ValidateValueAsync` methods.

```csharp
using System;
using System.ComponentModel.DataAnnotations;

public sealed class ValidVatNumberAttribute : AsyncValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext context) =>
        throw new InvalidOperationException("Validate this attribute with IsValidAsync.");

    protected override async Task<ValidationResult?> IsValidAsync(
        object? value, ValidationContext context, CancellationToken cancellationToken)
    {
        var registry = context.GetRequiredService<IVatRegistry>();
        return await registry.IsRegisteredAsync((string?)value, cancellationToken)
            ? ValidationResult.Success
            : new ValidationResult("That VAT number isn't registered with the tax authority.");
    }
}

public class Invoice
{
    [Required]
    [StringLength(14, MinimumLength = 8)]
    [RegularExpression(@"^[A-Z]{2}[A-Z0-9]+$")]
    [ValidVatNumber] // asynchronous
    public string VatNumber { get; set; } = "";
}

var context = new ValidationContext(model, serviceProvider, items: null);
await Validator.ValidateObjectAsync(model, context, validateAllProperties: true);
```

`Microsoft.Extensions.Options` gains matching support: options can be validated asynchronously, including at startup through the new `IAsyncStartupValidator`. This lets an app fail fast when an option that requires a network check is misconfigured. `IStartupValidator` is now obsolete; implement `IAsyncStartupValidator` instead.

Source-generated options validators provide a synchronous `IValidateOptions<TOptions>.Validate` implementation in addition to asynchronous validation, so existing synchronous validation call sites keep working alongside validators that need to await I/O:

```csharp
public sealed class BackendOptionsValidator : IAsyncValidateOptions<BackendOptions>
{
    public async Task<ValidateOptionsResult> ValidateAsync(
        string? name, BackendOptions options, CancellationToken cancellationToken = default) =>
        await IsReachableAsync(options.HostName, cancellationToken)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail($"'{options.HostName}' isn't reachable.");

    // Source-generated validators also implement this synchronous fallback.
    public ValidateOptionsResult Validate(string? name, BackendOptions options) =>
        ValidateOptionsResult.Fail("Backend validation must run asynchronously.");
}
```

### Format DataAnnotations messages

`ValidationAttribute.FormatMessage` lets a validation host supply a message template while the attribute fills in its own placeholders. This method supports localization without duplicating the formatting logic for built-in attributes such as <xref:System.ComponentModel.DataAnnotations.RangeAttribute>:

```csharp
using System.ComponentModel.DataAnnotations;

var range = new RangeAttribute(1, 10);
string message = range.FormatMessage("{0} must be between {1} and {2}", "Value");
// Value must be between 1 and 10
```

### Activity tracing configuration

A new tracing configuration API in `Microsoft.Extensions.Diagnostics` lets you turn `Activity` tracing on and off with rules instead of wiring up `ActivityListener` instances manually. Call `AddTracing` and describe which activity sources, operations, and listeners to enable or disable. Rules can be driven from configuration, so tracing can be tuned without a redeploy.

```csharp
builder.Services.AddTracing(tracing =>
{
    tracing.EnableTracing(sourceName: "MyCompany.Orders");
    tracing.DisableTracing(sourceName: "MyCompany.Orders", operationName: "HealthCheck");
});
```

The same release adds `ActivitySourceFactory` and unseals `ActivitySource`, which together support factory-driven creation and refreshable listeners.

### Generic source-generated logging methods

The logging source generator now supports generic methods decorated with <xref:Microsoft.Extensions.Logging.LoggerMessageAttribute>, primarily to avoid boxing. Generic type parameters can use standard constraints, such as `class`, `struct`, `unmanaged`, interfaces, base types, and `new()`. The `allows ref struct` anti-constraint isn't supported and produces [SYSLIB1011](../../../fundamentals/syslib-diagnostics/syslib1011.md).

```csharp
[LoggerMessage(0, LogLevel.Trace, "Received {Code} ({Length} bytes)")]
public static partial void PacketReceived<TCode>(ILogger logger, TCode code, int length)
    where TCode : struct, Enum;
```

### Improved reliability for configuration file change detection

The <xref:Microsoft.Extensions.FileProviders.Physical.PhysicalFilesWatcher> type, which configuration providers use to detect configuration file changes, includes several reliability improvements.

It now handles relative paths, missing directories, and file systems that don't support file watching more reliably. It also avoids watching subdirectories when it doesn't need to.

## Cryptography

- [X25519 Diffie-Hellman key exchange](#x25519-diffie-hellman-key-exchange)
- [CryptographicOperations.FixedTimeEquals overload](#cryptographicoperationsfixedtimeequals-overload)
- [AES Key Wrap](#aes-key-wrap)
- [Faster authenticated encryption on Apple platforms](#faster-authenticated-encryption-on-apple-platforms)
- [Experimental composite ML-KEM and HPKE APIs](#experimental-composite-ml-kem-and-hpke-apis)
- [Encrypt CMS messages with ML-KEM](#encrypt-cms-messages-with-ml-kem)

### X25519 Diffie-Hellman key exchange

The new <xref:System.Security.Cryptography.X25519DiffieHellman?displayProperty=fullName> abstract class provides a clean API for the X25519 elliptic-curve Diffie-Hellman algorithm, which is widely used in TLS 1.3, SSH, and Signal-protocol implementations. Platform-specific implementations are provided through <xref:System.Security.Cryptography.X25519DiffieHellmanCng> (Windows) and <xref:System.Security.Cryptography.X25519DiffieHellmanOpenSsl> (Linux and macOS), but the portable `GenerateKey()` factory method picks the right one automatically:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="X25519KeyExchange":::

The class supports the full key lifecycle: key generation, PKCS#8 and SubjectPublicKeyInfo import/export, PEM serialization, and raw private/public key access.

### CryptographicOperations.FixedTimeEquals overload

For secret-comparison scenarios where the expected value is a single known byte, use the new <xref:System.Security.Cryptography.CryptographicOperations.FixedTimeEquals*?displayProperty=nameWithType> overload that compares a <xref:System.ReadOnlySpan`1> with a single `byte` value in constant time:

```csharp
bool equal = CryptographicOperations.FixedTimeEquals(receivedSpan, 0x42);
```

### AES Key Wrap

<xref:System.Security.Cryptography.Aes?displayProperty=fullName> now supports the AES Key Wrap algorithms used by JOSE and other libraries to encrypt cryptographic keys:

- <xref:System.Security.Cryptography.Aes.EncryptKeyWrapPadded*?displayProperty=nameWithType>, <xref:System.Security.Cryptography.Aes.DecryptKeyWrapPadded*?displayProperty=nameWithType>, <xref:System.Security.Cryptography.Aes.TryDecryptKeyWrapPadded(System.ReadOnlySpan{System.Byte},System.Span{System.Byte},System.Int32@)?displayProperty=nameWithType>, and <xref:System.Security.Cryptography.Aes.GetKeyWrapPaddedLength(System.Int32)?displayProperty=nameWithType> implement the padded AES-KWP algorithm (RFC 5649).
- <xref:System.Security.Cryptography.Aes.EncryptKeyWrap*?displayProperty=nameWithType>, <xref:System.Security.Cryptography.Aes.DecryptKeyWrap*?displayProperty=nameWithType>, <xref:System.Security.Cryptography.Aes.TryDecryptKeyWrap(System.ReadOnlySpan{System.Byte},System.Span{System.Byte},System.Int32@)?displayProperty=nameWithType>, and <xref:System.Security.Cryptography.Aes.GetKeyWrapLength(System.Int32)?displayProperty=nameWithType> implement the unpadded AES Key Wrap algorithm (RFC 3394), which requires plaintext that's a multiple of 8 bytes.

Both sets of APIs provide array-returning and span-based overloads:

:::code language="csharp" source="./snippets/csharp/Libraries.cs" id="AesKeyWrap":::

### Faster authenticated encryption on Apple platforms

<xref:System.Security.Cryptography.AesGcm> and <xref:System.Security.Cryptography.ChaCha20Poly1305> avoid passing empty associated data to CryptoKit on Apple platforms, letting the native implementation use its empty-AAD fast path. On Apple platforms version 26 and later, encryption also skips a copy that was previously required for older Foundation implementations.

### Experimental composite ML-KEM and HPKE APIs

> [!WARNING]
> `CompositeMLKem` and `Hpke` are experimental in .NET 11 and might change. Direct use requires you to opt in to the `SYSLIB5006` diagnostic for `CompositeMLKem` and the `SYSLIB5009` diagnostic for `Hpke`.

`CompositeMLKem` combines post-quantum and classical key encapsulation so that both contribute to the shared secret. It's available on platforms that already support Module-Lattice-Based Key-Encapsulation Mechanism (ML-KEM). Call `CompositeMLKem.IsAlgorithmSupported` to determine whether the platform supports a particular combination of algorithms.

`Hpke` provides Hybrid Public Key Encryption (HPKE). The current implementation supports classical Diffie-Hellman key encapsulation mechanism (DHKEM) suites. ML-KEM and hybrid ML-KEM suites aren't supported yet. Check `Hpke.IsSupported` for the suite you select.

### Encrypt CMS messages with ML-KEM

The Cryptographic Message Syntax (CMS) API, <xref:System.Security.Cryptography.Pkcs.EnvelopedCms>, can encrypt and decrypt messages by using ML-KEM key encapsulation mechanism (KEM) recipients. Support is limited to the pure ML-KEM algorithm, and it's available only on non-Windows platforms.

## Networking and transport security

- [TLS handshake hardening](#tls-handshake-hardening)
- [Experimental caller-driven TLS sessions](#experimental-caller-driven-tls-sessions)
- [HTTP request compression](#http-request-compression)
- [Configurable HTTP connection eviction](#configurable-http-connection-eviction)
- [Initial HTTP/2 concurrency](#initial-http2-concurrency)
- [DNS record resolution APIs](#dns-record-resolution-apis)
- [HTTP/2 automatic downgrade for Windows authentication](#http2-automatic-downgrade-for-windows-authentication)
- [QUIC stream priority](#quic-stream-priority)
- [Video MIME type constants](#video-mime-type-constants)
- [HTTP metrics use observable instruments](#http-metrics-use-observable-instruments)

### TLS handshake hardening

Two <xref:System.Net.Security?displayProperty=fullName> items improve TLS (Transport Layer Security) reliability:

- <xref:System.Net.Security.SslStream> server-side handshake bounds-checking fixes in `TlsFrameHelper` close several edge cases that could surface as `IOException` on malformed ClientHello records.
- On Linux, certificate-validation failures now surface as standard TLS alerts to the peer, matching Windows behavior. Connecting clients receive an actionable handshake error instead of a connection drop.

Negotiate authentication servers can also validate TLS channel binding tokens on Unix. The managed GSSAPI path passes the channel binding data to `gss_accept_sec_context`, which rejects a mismatch in the same way as the existing Unix client path. This enables Extended Protection scenarios, such as ASP.NET Core's Negotiate authentication, that bind authentication to the underlying TLS connection.

### Experimental caller-driven TLS sessions

> [!WARNING]
> The `TlsContext`, `TlsSession`, `TlsBufferSession`, `TlsSocketSession`, and `TlsOperationStatus` types are experimental in .NET 11 and report diagnostic `SYSLIB5007`.

<xref:System.Net.Security?displayProperty=fullName> adds a caller-driven, non-blocking TLS state machine for advanced transports where the application controls buffers and I/O scheduling. `TlsBufferSession` operates over caller-provided spans, while `TlsSocketSession` works with a `SafeSocketHandle`.

Operations such as `Handshake`, `Read`, `Write`, `Shutdown`, and `RequestClientCertificate` return `TlsOperationStatus`, which reports whether an operation completed, needs more input, needs a larger destination buffer, or requires certificate validation or a new `TlsContext`.

### HTTP request compression

<xref:System.Net.Http?displayProperty=fullName> adds <xref:System.Net.Http.GZipCompressedContent>, <xref:System.Net.Http.BrotliCompressedContent>, and <xref:System.Net.Http.ZstandardCompressedContent> wrappers for request bodies. These wrappers set the `Content-Encoding` header and stream compressed content as the request serializes.

### Configurable HTTP connection eviction

<xref:System.Net.Http.SocketsHttpHandler> now provides <xref:System.Net.Http.SocketsHttpHandler.ShouldEvictConnection?displayProperty=nameWithType> for per-connection eviction decisions. This callback lets you retire connections based on age, endpoint details, and DNS changes instead of fixed lifetime-only policies.

### Initial HTTP/2 concurrency

`SocketsHttpHandler.InitialHttp2MaxConcurrentStreams` limits the number of concurrent streams on each new HTTP/2 connection before the server's `SETTINGS` frame arrives. If you know that a server's stream limit is lower than the default of 100, set a lower value to limit the initial burst of requests on that connection. After the connection receives the `SETTINGS` frame, it uses the server's advertised limit.

The property requires a value of at least 1. If an earlier connection to the same host advertised a lower limit, that limit can further reduce the initial concurrency for the host.

```csharp
var handler = new SocketsHttpHandler
{
    InitialHttp2MaxConcurrentStreams = 20
};
using var client = new HttpClient(handler);
```

### DNS record resolution APIs

<xref:System.Net.Dns?displayProperty=fullName> adds typed record-resolution APIs, including <xref:System.Net.Dns.ResolveSrv(System.String)?displayProperty=nameWithType>, <xref:System.Net.Dns.ResolveMx(System.String)?displayProperty=nameWithType>, <xref:System.Net.Dns.ResolveTxt(System.String)?displayProperty=nameWithType>, <xref:System.Net.Dns.ResolveCName(System.String)?displayProperty=nameWithType>, <xref:System.Net.Dns.ResolvePtr(System.String)?displayProperty=nameWithType>, and <xref:System.Net.Dns.ResolveNs(System.String)?displayProperty=nameWithType>, together with `Async` variants. The results include records, response code, and negative-cache time-to-live (TTL) metadata through <xref:System.Net.DnsResult`1>.

The new <xref:System.Net.DnsResolver> class provides the same record types through an instance API that accepts a <xref:System.Net.DnsResolverOptions> object, which lets you supply a custom list of DNS servers instead of using the platform resolver configuration. `DnsResolver` and the static `Dns.Resolve*` methods now work on Linux in addition to Windows and macOS.

### HTTP/2 automatic downgrade for Windows authentication

<xref:System.Net.Http.HttpClient> automatically downgrades to HTTP/1.1 when a request requires Windows authentication (NTLM/Negotiate) over HTTP/2. The HTTP/2 specification disallows the connection-bound authentication schemes that NTLM and Kerberos rely on, so these requests previously failed. With the downgrade in place, applications targeting mixed-authentication environments—common in enterprise intranets—work without explicit `HttpRequestMessage.Version` overrides.

### QUIC stream priority

<xref:System.Net.Quic.QuicStream> gains a <xref:System.Net.Quic.QuicStream.Priority?displayProperty=nameWithType> property and a <xref:System.Net.Quic.QuicStream.DefaultPriority?displayProperty=nameWithType> constant that exposes HTTP/3 stream prioritization (RFC 9218). Priority values range from 0 (highest) to 255 (lowest), with a default of 127:

```csharp
// Increase priority for interactive streams
stream.Priority = 64; // Higher priority than the default 127
```

### Video MIME type constants

A new <xref:System.Net.Mime.MediaTypeNames.Video?displayProperty=nameWithType> class provides string constants for common video media types:

- `MediaTypeNames.Video.Mp4` — `"video/mp4"`
- `MediaTypeNames.Video.Mpeg` — `"video/mpeg"`
- `MediaTypeNames.Video.Ogg` — `"video/ogg"`
- `MediaTypeNames.Video.QuickTime` — `"video/quicktime"`
- `MediaTypeNames.Video.WebM` — `"video/webm"`

These join the existing `MediaTypeNames.Application`, `MediaTypeNames.Image`, `MediaTypeNames.Text`, and `MediaTypeNames.Multipart` classes.

### HTTP metrics use observable instruments

The high-cardinality HTTP `open_connections` and `active_requests` metrics are now observable instruments instead of regular counters. If you use <xref:System.Diagnostics.Metrics.MeterListener> directly to read these metrics, call <xref:System.Diagnostics.Metrics.MeterListener.RecordObservableInstruments?displayProperty=nameWithType> to receive measurements from them.

## Unsafe API accessibility

.NET 11 removes the `[RequiresUnsafe]` attribute from a large set of APIs that take pointer parameters. Previously, calling these methods from code that used `unsafe` blocks still required a project-level `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` setting because the attribute enforced that requirement independently. Now, only the standard `unsafe` block or method modifier is required.

Affected APIs include:

- <xref:System.Buffer.MemoryCopy*?displayProperty=nameWithType>
- <xref:System.ReadOnlySpan`1.%23ctor(System.Void*,System.Int32)?displayProperty=nameWithType> and <xref:System.Span`1.%23ctor(System.Void*,System.Int32)?displayProperty=nameWithType>
- <xref:System.Runtime.CompilerServices.Unsafe?displayProperty=fullName> pointer methods such as `AsRef`, `Read`, `Write`, and `Copy`
- <xref:System.Runtime.InteropServices.NativeMemory?displayProperty=fullName> methods
- <xref:System.Text.Encoding?displayProperty=fullName> pointer overloads (all encoding classes)
- <xref:System.Numerics.Vector?displayProperty=fullName> pointer-based `Load` and `Store` methods
- Interop marshalling types in <xref:System.Runtime.InteropServices.Marshalling?displayProperty=fullName>

This change reduces friction for advanced scenarios such as interop authoring, memory-mapped files, and low-level buffer manipulation, where unsafe context is already required by the calling code.

## See also

- [What's new in the .NET 11 runtime](runtime.md)
- [What's new in the SDK for .NET 11](sdk.md)
- [Breaking changes in .NET 11](../../compatibility/11.md)
