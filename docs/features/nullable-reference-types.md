Nullable Reference Types
=========
Reference types may be nullable, non-nullable, or null-oblivious (abbreviated here as `?`, `!`, and `~`).

## Setting project level nullable context

Project level nullable context can be set by using "nullable" command line switch:
-nullable[+|-]                        Specify nullable context option enable|disable.
-nullable:{enable|disable|warnings}   Specify nullable context option enable|disable|warnings.

Through msbuild the context could be set by supplying an argument for a "Nullable" parameter of Csc build task.
Accepted values are "enable", "disable", "warnings", or null (for the default nullable context according to the compiler).
The Microsoft.CSharp.Core.targets passes value of msbuild property named "Nullable" for that parameter.

Note that in previous preview releases of C# 8.0 this "Nullable" property was successively named "NullableReferenceTypes" then "NullableContextOptions".

## Annotations
In source, nullable reference types are annotated with `?`.
```c#
string? OptString; // may be null
Dictionary<string, object?>? OptDictionaryOptValues; // dictionary may be null, values may be null
```
A warning is reported when annotating a reference type with `?` outside a `#nullable` context.

[Metadata representation](nullable-metadata.md)

## Declaration warnings
_Describe warnings reported for declarations in initial binding._

## Flow analysis
Flow analysis is used to infer the nullability of variables within executable code. The inferred nullability of a variable is independent of the variable's declared nullability.
Method calls are analyzed even when they are conditionally omitted. For instance, `Debug.Assert` in release mode.

### Warnings
_Describe set of warnings. Differentiate W warnings._
If the analysis determines that a null check always (or never) passes, a hidden diagnostic is produced. For example: `"string" is null`.

### Null tests
A number of null checks affect the flow state when tested for:
- comparisons to `null`: `x == null` and `x != null`
- `is` operator: `x is null`, `x is K` (where `K` is a constant), `x is string`, `x is string s`
- calls to well-known equality methods, including:
  - `static bool object.Equals(object, object)`
  - `static bool object.ReferenceEquals(object, object)`
  - `bool object.Equals(object)` and overrides
  - `bool IEquatable<T>.Equals(T)` and implementations
  - `bool IEqualityComparer<T>.Equals(T, T)` and implementations

Some null checks are "pure null tests", which means that they can cause a variable whose flow state was previously not-null to update to maybe-null. Pure null tests include:
- `x == null`, `x != null` *whether using a built-in or user-defined operator*
- `(Type)x == null`, `(Type)x != null`
- `x is null`
- `object.Equals(x, null)`, `object.ReferenceEquals(x, null)`
- `IEqualityComparer<Type?>.Equals(x, null)`

All of the above checks except for `x is null` are commutative. For example, `null == x` is also a valid pure null test.

Some expressions which may not return `bool` are also considered pure null tests:
- `x?.Member` will change the receiver's flow state to maybe-null unconditionally
- `x ?? y` will change the LHS expression's flow state to maybe-null unconditionally

Example of how a pure null test can affect flow analysis:
```cs
string? s = "hello";
if (s != null)
{
    _ = s.ToString(); // ok
}
else
{
    _ = s.ToString(); // warning
}
```

Versus a "not pure" null test:
```cs
string? s = "hello";
if (s is string)
{
    _ = s.ToString(); // ok
}
else
{
    _ = s.ToString(); // ok
}
```

Invocation of methods annotated with the following attributes will also affect flow analysis:
- simple pre-conditions: `[AllowNull]` and `[DisallowNull]`
- simple post-conditions: `[MaybeNull]` and `[NotNull]`
- conditional post-conditions: `[MaybeNullWhen(bool)]` and `[NotNullWhen(bool)]`
- `[DoesNotReturnIf(bool)]` (e.g. `[DoesNotReturnIf(false)]` for `Debug.Assert`) and `[DoesNotReturn]`
- `[NotNullIfNotNull(string)]`
See https://github.com/dotnet/csharplang/blob/master/meetings/2019/LDM-2019-05-15.md

The `Interlocked.CompareExchange` methods have special handling in flow analysis instead of being annotated due to the complexity of their nullability semantics. The affected overloads include:
- `static object? System.Threading.Interlocked.CompareExchange(ref object? location, object? value, object? comparand)`
- `static T System.Threading.Interlocked.CompareExchange<T>(ref T location, T value, T comparand) where T : class?`

## `default`
If `T` is a reference type, `default(T)` is `T?`.
```c#
string? s = default(string); // assigns ?, no warning
string t = default; // assigns ?, warning
```
If `T` is a value type, `default(T)` is `T` and any non-value-type fields in `T` are maybe null.
If `T` is a value type, `new T()` is equivalent to `default(T)`.
```c#
struct Pair<T, U> { public T First; public U Second; }
var p = default(Pair<object?, string>); // ok: Pair<object?, string!> p
p.Second.ToString(); // warning
(object?, string) t = default; // ok
t.Item2.ToString(); // warning
```
If `T` is an unconstrained type parameter, `default(T)` is a `T` that is maybe null.
```c#
T t = default; // warning
```
_Is `default(T?)` an error?_

### Conversions
Conversions can be calculated with ~ considered distinct from ? and !, or with ~ implicitly convertible to ? and !.
Given `IIn<in T>` and `IOut<out T>`, with ~ distinct:
- `T!` is a `T~` is a `T?`
- `IIn<T!>` is a `IIn<T~>` is a `IIn<T?>`
- `IOut<T?>` is a `IOut<T~>` is a `IOut<T!>`
Most conversions are considered with ~ implicitly convertible to ? and !.

_Describe warnings from user-defined conversions._

### Assignment
For `x = y`, the nullability of the converted type of `y` is used for `x`.
A warning is reported if there is a mismatch between top-level or nested nullability comparing the inferred nullability of `x` and the declared type of `y`.
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

### Suppression operator (`!`)
The postfix `!` operator sets the top-level nullability to non-nullable. Conversions on a suppressed expression produce no nullability warnings.
```c#
var x = optionalString!; // x is string!
var y = obliviousString!; // y is string!
var z = new [] { optionalString, obliviousString }!; // no change, z is string?[]!
```
A warning is reported when using the `!` operator absent a `NonNullTypes` context.

Unnecessary usages of `!` do not produce any diagnostics, including `!!`.

A suppressed expression `e!` can be target-typed if the operand expression `e` can be target-typed.

### Explicit cast
Explicit cast to `?` changes top-level nullability.
Explicit cast to `!` does not change top-level nullability and may produce W warning.
```c#
var x = (string)maybeNull; // x is string?, W warning
var y = (string)oblivious; // y is string~, no warning
var z = (string?)notNull; // y is string?, no warning
```
A warning is reported if the type of operand including nested nullability is not explicitly convertible to the target type.
```c#
sealed class MyList<T> : IEnumerable<T> { }
var x =  new MyList<string?>();
var y = (IEnumerable<object>)x;  // warning
var z = (IEnumerable<object?>)x; // no warning
```

### Method type inference

We modify the spec rule for [Fixing](https://github.com/dotnet/csharplang/blob/master/spec/expressions.md#fixing "Fixing") to take account of types that may be equivalent (i.e. have an identity conversion) yet may not be identical in the set of bounds. The existing spec says (third bullet)

> If among the remaining candidate types `Uj` there is a unique type `V` from which there is an implicit conversion to all the other candidate types, then `Xi` is fixed to `V`.

This is not correct for C# 5 (i.e., it is a bug in the language specification), as it fails to handle bounds such as `Dictionary<object, dynamic>` and `Dictionary<dynamic, object>`, which are merged to `Dictionary<dynamic, dynamic>`. This is [an open issue](https://github.com/ECMA-TC49-TG2/spec/issues/951) that we anticipate will be addressed in the next iteration of the ECMA specification.  Handling nullability properly will require additional changes beyond those in that next iteration of the ECMA spec.

When adding to the set of exact bounds of a type parameter, types that are equivalent (i.e. have an identity conversion) but not identical are merged using *invariant* rules.  When adding to the set of lower bounds of a type parameter, types that are equivalent but not identical are merged using *covariant* rules.  When adding to the set of upper bounds of a type parameter, types that are equivalent but not identical are merged using *contravariant* rules.  In the final fixing step, types that are equivalent but not identical are merged using *covariant* rules.

Merging equivalent but not identical types is done as follows:

#### Invariant merging rules

- Merging `dynamic` and `object` results in the type `dynamic`.
- Merging tuple types that differ in element names is specified elsewhere.
- Merging equivalent types that differ in nullability is performed as follows: merging the types `Tn` and `Um` (where `n` and `m` are differing nullability annotations) results in the type `Vk` where `V` is the result of merging `T` and `U` using the invariant rule, and `k` is as follows:
 - if either `n` or `m` are non-nullable, non-nullable.
 - if either `n` or `m` are nullable, nullable.
 - otherwise oblivious.
- Merging constructed generic types is performed as follows: Merging the types `K<A1, A2, ...>` and `K<B1, B2, ...>` results in the type `K<C1, C2, ...>` where `Ci` is the result of merging `Ai` and `Bi` by the invariant rule.
- Merging tuple types `(A1, A2, ...)` and `(B1, B2, ...)` results in the type `(C1, C2, ...)` where `Ci` is the result of merging `Ai` and `Bi` by the invariant rule.
- Merging the array types `T[]` and `U[]` results in the type `V[]` where `V` is the result of merging `T` and `U` by the invariant rule.

#### Covariant merging rules

- Merging `dynamic` and `object` results in the type `dynamic`.
- Merging tuple types that differ in element names is specified elsewhere.
- Merging equivalent types that differ in nullability is performed as follows: merging the types `Tn` and `Um` (where `n` and `m` are differing nullability annotations) results in the type `Vk` where `V` is the result of merging `T` and `U` using the covariant rule, and `k` is as follows:
 - if either `n` or `m` are nullable, nullable.
 - if either `n` or `m` are oblivious, oblivious.
 - otherwise non-nullable.
- Merging constructed generic types is performed as follows: Merging the types `K<A1, A2, ...>` and `K<B1, B2, ...>` results in the type `K<C1, C2, ...>` where `Ci` is the result of merging `Ai` and `Bi` by
  - the invariant rule if `K`'s type parameter in the `i` position is invariant.
  - the covariant rule if `K`'s type parameter in the `i` position is declared `out`.
  - the contravariant rule if the `K`'s type parameter in the `i` position is declared `in`.
- Merging tuple types `(A1, A2, ...)` and `(B1, B2, ...)` results in the type `(C1, C2, ...)` where `Ci` is the result of merging `Ai` and `Bi` by the covariant rule.
- Merging the array types `T[]` and `U[]` results in the type `V[]` where `V` is the result of merging `T` and `U` by the invariant rule.

#### Contravariant merging rules

- Merging `dynamic` and `object` results in the type `dynamic`.
- Merging tuple types that differ in element names is specified elsewhere.
- Merging equivalent types that differ in nullability is performed as follows: merging the types `Tn` and `Um` (where `n` and `m` are differing nullability annotations) results in the type `Vk` where `V` is the result of merging `T` and `U` using the contravariant rule, and `k` is as follows:
 - if either `n` or `m` are non-nullable, non-nullable.
 - if either `n` or `m` are oblivious, oblivious.
 - otherwise nullable.
- Merging constructed generic types is performed as follows: Merging the types `K<A1, A2, ...>` and `K<B1, B2, ...>` results in the type `K<C1, C2, ...>` where `Ci` is the result of merging `Ai` and `Bi` by
  - the invariant rule if `K`'s type parameter in the `i` position is invariant.
  - the covariant rule if `K`'s type parameter in the `i` position is declared `in`.
  - the contravariant rule if `K`'s type parameter in the `i` position is declared `out`.
- Merging tuple types `(A1, A2, ...)` and `(B1, B2, ...)` results in the type `(C1, C2, ...)` where `Ci` is the result of merging `Ai` and `Bi` by the contravariant rule.
- Merging the array types `T[]` and `U[]` results in the type `V[]` where `V` is the result of merging `T` and `U` by the invariant rule.

It is intended that these merging rules are associative and commutative, so that a compiler may merge a set of equivalent types pairwise in any order to compute the final result.

> ***Open issue***: these rules do not describe the handling of merging a nested generic type `K<A>.L<B>` with `K<C>.L<D>`. That should be handled the same as a hypothetical type `KL<A, B>` would be merged with `KL<C, D>`.

> ***Open issue***: these rules do not describe the handling of merging pointer types.

### Array creation
The calculation of the _best type_ element nullability uses the Conversions rules above and the covariant merging rules.
```c#
var w = new [] { notNull, oblivious }; // ~[]!
var x = new [] { notNull, maybeNull, oblivious }; // ?[]!
var y = new [] { enumerableOfNotNull, enumerableOfMaybeNull, enumerableOfOblivious }; // IEnumerable<?>!
var z = new [] { listOfNotNull, listOfMaybeNull, listOfOblivious }; // List<~>!
```

### Anonymous types
Fields of anonymous types have nullability of the arguments, inferred from the initializing expression.
```c#
static void F<T>(T x, T y)
{
    if (x == null) return;
    var a = new { x, y }; // inferred as x:T, y:T (both Unannotated)
    a.x.ToString(); // ok (non-null tracked by the nullable flow state of the initial value for property x)
    a.y.ToString(); // warning
}
```

### Null-coalescing operator
The top-level nullability of `x ?? y` is `!` if `x` is `!` and otherwise the top-level nullability of `y`.
A warning is reported if there is a nested nullability mismatch between `x` and `y`.

### Try-finally
We infer the state after a try statement in part by keeping track of which variables may have been assigned a null value in the finally block.
This tracking distinguishes actual assignments from inferences:
only actual assignments (but not inferences) affect the state after the try statement.
See https://github.com/dotnet/roslyn/issues/34018 and https://github.com/dotnet/roslyn/pull/35276.
This is not yet reflected in the language specification for nullable reference types
(as we don't have a specification for how to handle try statements at this time).

## Type parameters
A `class?` constraint is allowed, which, like class, requires the type argument to be a reference type, but allows it to be nullable.
[Nullable strawman](https://github.com/dotnet/csharplang/issues/790)
[4/25/18](https://github.com/dotnet/csharplang/blob/master/meetings/2018/LDM-2018-04-25.md)

Explicit `object` (or `System.Object`) constraints of any nullability are disallowed. However, type substitution can lead to
`object!` or `object~` constraints to appear among the constraint types, when their nullability is significant by comparison to
other constraints. An `object!` constraint requires the type to be non-nullable (value or reference type).
However, an explicit `object?` constraint is not allowed.
An unconstrained (here it means - no type constraints, and no `class`, `struct`, or `unmanaged` constraints) type parameter is essentially
equivalent to one constrained by `object?` when it is declared in a context where nullable annotations are enabled. If annotations are disabled,
the type parameter is essentially equivalent to one constrained by `object~`. The context is determined at the identifier that declares the type
parameter within a type parameter list.
[4/25/18](https://github.com/dotnet/csharplang/blob/master/meetings/2018/LDM-2018-04-25.md)
Note, the `object`/`System.Object` constraint is represented in metadata as any other type constraint, the type is System.Object.

An explicit `notnull` constraint is allowed, which requires the type to be non-nullable (value or reference type).
[5/15/19](https://github.com/dotnet/csharplang/blob/master/meetings/2019/LDM-2019-05-15.md)
The rules to determine when it is a named type constraint or a special `notnull` constraint are similar to rules for `unmanaged`. Similarly, it is valid only
at the first position in constraints list.

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
An error is reported for duplicate constraints where constraints are compared ignoring top-level and nested nullability.
```c#
class C<T> where T : class
{
    static void F1<U>() where U : T, T? { } // error: duplicate constraint
    static void F2<U>() where U : I<T>, I<T?> { } // error: duplicate constraint
}
```
_What are the rules for annotated (unannotated) type arguments for generic type parameters from unannotated (annotated) types and methods?_
```c#
[NotNullTypes(false)] List<T> F1<T>(T t) where T : class { ... }
[NotNullTypes(true)]  List<T> F2<T>(T t) where T : class { ... }
[NotNullTypes(true)]  List<T?> F3<T>(T? t) where T : class { ... }
var x = F1(notNullString);   // List<string!> or List<string~> ?
var y = F1(maybeNullString); // List<string?> or List<string~> ?
var z = F2(obliviousString); // List<string~>! or List<string!>! ?
var w = F3(obliviousString); // List<string~>! or List<string?>! ?
```
A warning is reported for dereferencing variables of type T, where T is an unconstrained type parameter.
```C#
static void F<T>(T t) => t.ToString(); // Warn possible null dereference
```
A warning is reported when attempting to convert a null, default, or potentially null value to an unconstrained type parameter.
```C#
static T F<T>() => default; // Warn converting default to T
```

## Object creation
An error is reported for creating an instance of a nullable reference type.
```c#
new C?(); // error
new List<C?>(); // ok
```

## Generated code
Older code generation strategies may not be nullable aware.
Setting the project-level nullable context to "enable" could result in many warnings that a user is unable to fix.
To support this scenario any syntax tree that is determined to be generated will have its nullable state implicitly set to "disable", regardless of the overall project state.

A syntax tree is determined to be generated if meets one or more of the following criteria:
- File name begins with:
    - TemporaryGeneratedFile_
- File name ends with:
    - .designer.cs
    - .generated.cs
    - .g.cs
    - .g.i.cs
- Contains a top level comment that contains
    - `<autogenerated`
    - `<auto-generateed`

Newer, nullable-aware generators may then opt-in to nullable analysis by including a generated `#nullable restore` at the beginning of the code, ensuring that the generated syntax tree will be analyzed according to the project-level nullable context.

## Public APIs
There are a few questions that an API consumer would want to answer:
1. should I print a `?` after the type?
2. can I assign a `null` value to a variable of this type?
3. could I read a `null` value from a variable of this type?

Two primitive concepts we wish to expose are: `IsAnnotated` and `NonNullTypes`.
_We may expose some higher-level concepts (to address questions 2 and 3 conveniently) as well._

|  | IsAnnotated |
|--| ----------- |
| `string?` | `true` |
| `int?` / `Nullable<int>` | `true` (_needs confirmation_) |
| `Nullable<T>` | `true` |
| `T? where T : class` | `true` |
| `T? where T : struct` | `true` (_needs confirmation_) |
| `string` | `false` |
| `int` | `false` |
| `T where T : class/object` | `false` |
| `T where T : class?` | `false` |
| `T where T : struct` | `false` |
| `T` | `false` |
