Nullable Reference Types
=========
References to reference types may be nullable, non-nullable, or null-oblivious. (The document uses the shorthand `?`, `!`, and `~`.)

## Annotations
In source, nullable reference types are annotated with `?`.
```c#
string? OptString; // may be null
List<object?>? OptListOptElements; // list may be null, elements may be null
```
In metadata, nullable reference types are annotated with a `[Nullable]` attribute.
```c#
namespace System.Runtime.CompilerServices
{
    public sealed class NullableAttribute : Attribute
    {
        public NullableAttribute() { }
        public NullableAttribute(bool[] b) { }
    }
}
```
If annotations are enabled in a module, a `[module: Nullable]` attribute is included.
```c#
// C# representation of metadata
[module: Nullable]
[Nullable] string OptString; // string?
[Nullable(new[] { true, true })] List<object> OptListOptElements; // List<object?>?
```
`NullableAttribute` cannot be used explicitly in source so the type declaration can be synthesized by the compiler if it is not included in the compilation.

Unannotated reference types are non-nullable or null-oblivious depending whether the containing scope includes `[NonNullTypes]`.
```c#
namespace System.Runtime.CompilerServices
{
    public sealed class NonNullTypesAttribute : Attribute
    {
        public NonNullTypesAttribute(bool enabled = true) { }
    }
}
```
If there is no `[NonNullTypes]` attribute in any containing scope, including the module, reference types are non-nullable.
```c#
[NonNullTypes(false), Nullable(new[] { false, true })] string[] Oblivious; // string?[]~
[NonNullTypes(true), Nullable(new[] { false, true })] string[] NotNull; // string?[]!
```
`NonNullTypesAttribute` can be referenced in source so the type declaration must be provided explicitly to the compilation and should be defined in the framework.

## Declaration warnings
_Describe warnings reported for declarations in initial binding._

## Flow analysis
Flow analysis is used to infer the nullability of variables within executable code. The inferred nullability of a variable is independent of it's declared nullability.

### Warnings
_Describe set of warnings. Differentiate W warnings._

### Null tests
_Describe the set of tests that affect flow state._

### Conversions
_Describe valid top-level and variance conversions._
_Describe warnings from user-defined conversions._

### Assignment
Nullability of the righthand side flows to the lefthand side unchanged. Warnings are reported for top-level and nested nullability mismatches.
```c#
notNull = maybeNull; // assigns ?; warning
notNull = oblivious; // assigns ~, no warning
oblivious = maybeNull; // assigns ?, no warning
```

### Local declarations
Nullablilty follows from assignment.
```c#
string notNull = maybeNull; // assigns ?; warning
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

### Explicit cast
Explicit cast to `?` changes top-level nullability.
Explicit cast to `!` does not change top-level nullability and may produce W warning.
```c#
var x = (string)maybeNull; // x is string?, W warning
var y = (string)oblivious; // y is string~, no warning
var z = (string?)notNull; // y is string?, no warning
```
Explicit casts change nested nullability without warning.
```c#
List<object?> x = ...;
var y = (List<object>)x; // y is List<object!>!, no warning
```

### Array creation
The _best type_ calculation uses the most relaxed nullability: `?` is more relaxed than `~` which is more relaxed than `!`.
If there is no best nested nullability, a warning is reported.
```c#
var x = new [] { notNull, maybeNull, oblivious }; // ~[]!
var y = new [] { enumerableOfNotNull, enumerableOfMaybeNull, enumerableOfOblivious }; // IEnumerable<?>!
var z = new [] { listOfNotNull, listOfMaybeNull, listOfOblivious }; // List<~>!, warning
```

### Null-coalescing operator
_Describe_

## Type parameters
_Describe_
