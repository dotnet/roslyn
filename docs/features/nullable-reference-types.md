Nullable Reference Types
=========
Reference types may be nullable, non-nullable, or null-oblivious (abbreviated here as `?`, `!`, and `~`).

## Annotations
In source, nullable reference types are annotated with `?`.
```c#
string? OptString; // may be null
Dictionary<string, object?>? OptDictionaryOptValues; // dictionary may be null, values may be null
```
In metadata, nullable reference types are annotated with a `[Nullable]` attribute.
```c#
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.Class |
        AttributeTargets.GenericParameter |
        AttributeTargets.Event | AttributeTargets.Field | AttributeTargets.Property |
        AttributeTargets.Parameter | AttributeTargets.ReturnValue,
        AllowMultiple = false)]
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
        public NullableAttribute(bool[] b) { }
    }
}
```
The parameter-less constructor is emitted for simple type references with top-level nullability;
the constructor with `bool[]` parameter is emitted for type references with nested types and nullability.
```c#
// C# representation of metadata
[Nullable]
string OptString; // string?
[Nullable(new[] { true, false, true })]
Dictionary<string, object> OptDictionaryOptValues; // Dictionary<string!, object?>?
```
The `NullableAttribute` type declaration is synthesized by the compiler if it is not included in the compilation.

Unannotated reference types are non-nullable or null-oblivious depending on whether the containing scope includes `[NonNullTypes]`.
```c#
namespace System.Runtime.CompilerServices
{
    [AttributeUsage(
        AttributeTargets.All,
        AllowMultiple = false)]
    public sealed class NonNullTypesAttribute : Attribute
    {
        public NonNullTypesAttribute(bool enabled = true) { }
    }
}
```
If there is no `[NonNullTypes]` attribute at any containing scope, including the module, reference types are null-oblivious.
```c#
[NonNullTypes(false)] string[] Oblivious; // string~[]~
[NonNullTypes(true)] string[] NotNull; // string![]!
[NonNullTypes(false), Nullable(new[] { false, true })] string[] ObliviousMaybeNull; // string?[]~
[NonNullTypes(true), Nullable(new[] { false, true })] string[] NotNullMaybeNull; // string?[]!
```
`NonNullTypesAttribute` is not synthesized by the compiler. If the attribute is used explicitly in source, the type declaration must be provided explicitly to the compilation. The type should be defined in the framework.

## Declaration warnings
_Describe warnings reported for declarations in initial binding._

## Flow analysis
Flow analysis is used to infer the nullability of variables within executable code. The inferred nullability of a variable is independent of the variable's declared nullability.

### Warnings
_Describe set of warnings. Differentiate W warnings._

### Null tests
_Describe the set of tests that affect flow state._

## `default`
`default(T)` is `T?` if `T` is a reference type.
_Is `default(T)` also `T?` if `T` is an unconstrained type parameter?_
_Is `default(T?)` an error?_
```c#
string? s = default(string); // assigns ?, no warning
string t = default; // assigns ?, warning
```

### Conversions
_Describe valid top-level and variance conversions._
_Describe warnings from user-defined conversions._

### Assignment
For `x = y`, the nullability of the converted type of `y` is used for `x`.
Warnings are reported if there is a mismatch between top-level or nested nullability comparing the inferred nullability of `x` and the declared type of `y`.
The warning is a W warning when assigning `?` to `!` and the target is a local.
```c#
notNull = maybeNull; // assigns ?, warning
notNull = oblivious; // assigns ~, no warning
oblivious = maybeNull; // assigns ?, no warning
```

### Local declarations
Nullablilty follows from assignment above. Assigning `?` to `!` is a W warning.
```c#
string notNull = maybeNull; // assigns ?, warning
```
Nullability of `var` declarations is determined from flow analysis.
```c#
var s = maybeNull; // s is ?, no warning
if (maybeNull == null) return;
var t = maybeNull; // t is !
```

### `!` operator
The postfix `!` operator sets the top-level nullability to non-nullable.
```c#
var x = optionalString!; // x is string!
var y = obliviousString!; // y is string!
var z = new [] { optionalString, obliviousString }!; // no change, z is string?[]!
```
_Should `!` suppress warnings for nested nullability?_
_Should `nonNull!` result in a warning for unnecessary `!`?_
_Should `!!` be an error?_

### Explicit cast
Explicit cast to `?` changes top-level nullability.
Explicit cast to `!` does not change top-level nullability and may produce W warning.
```c#
var x = (string)maybeNull; // x is string?, W warning
var y = (string)oblivious; // y is string~, no warning
var z = (string?)notNull; // y is string?, no warning
```
Explicit casts change nested nullability.
_Should a warning be reported when there is not an implicit nullable conversion (for instance `(List<object>)new List<object?>()`)?_
```c#
List<object?> x = ...;
var y = (List<object>)x; // y is List<object!>!, no warning
```

### Array creation
The _best type_ calculation uses the most relaxed nullability: `T!` is a `T~` is a `T?`.
If there is no best nested nullability, a warning is reported.
```c#
var w = new [] { notNull, oblivious }; // ~[]!
var x = new [] { notNull, maybeNull, oblivious }; // ?[]!
var y = new [] { enumerableOfNotNull, enumerableOfMaybeNull, enumerableOfOblivious }; // IEnumerable<?>!
var z = new [] { listOfNotNull, listOfMaybeNull, listOfOblivious }; // List<~>!, warning
```

### Null-coalescing operator
The top-level nullability of `x ?? y` is `!` if `x` is `!` and otherwise the top-level nullability of `y`.
A warning is reported if there is a nested nullability mismatch between `x` and `y`.

## Type parameters
A warning is reported for nullable type argument for type parameter with `class` constraint or non-nullable reference type or interface type constraint.
[4/25/18](https://github.com/dotnet/csharplang/blob/master/meetings/2018/LDM-2018-04-25.md)
```c#
static void F1<T>() where T : class { }
static void F2<T>() where T : Stream { }
static void F3<T>() where T : IDisposable { }
F1<Stream?>(); // warning
F2<Stream?>(); // warning
F3<Stream?>(); // warning
```
Type parameter constraints may include nullable reference type and interface types.
[4/25/18](https://github.com/dotnet/csharplang/blob/master/meetings/2018/LDM-2018-04-25.md)
```c#
static void F2<T> where T : Stream? { }
static void F3<T>() where T : IDisposable? { }
F2<Stream?>(); // ok
F3<Stream?>(); // ok
```
A warning is reported for inconsistent top-level nullability of constraint types.
[4/25/18](https://github.com/dotnet/csharplang/blob/master/meetings/2018/LDM-2018-04-25.md)
```c#
static void F4<T> where T : class, Stream? { } // warning
static void F5<T> where T : Stream?, IDisposable { } // warning
```
## Compiler switch
_Describe behavior when feature is disabled._
