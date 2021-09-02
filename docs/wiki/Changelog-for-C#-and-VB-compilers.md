

## General information
* [Compiler and language feature status](https://github.com/dotnet/roslyn/blob/main/docs/Language%20Feature%20Status.md)
* [Log of breaking changes](https://github.com/dotnet/roslyn/blob/main/docs/compilers/CSharp/Compiler%20Breaking%20Changes%20-%20post%20VS2017.md)
* [NuGet packages](https://github.com/dotnet/roslyn/blob/main/docs/wiki/NuGet-packages.md)
* [C# language version history](https://github.com/dotnet/csharplang/blob/main/Language-Version-History.md)

## Visual Studio 2017 Version 15.7

The C# compiler now supports the 7.3 set of language features including:
- `System.Enum`, `System.Delegate` and [`unmanaged`](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/blittable.md) constraints.
- [Ref local re-assignment](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/ref-local-reassignment.md): Ref locals and ref parameters can now be reassigned with the ref assignment operator (`= ref`).
- [Stackalloc initializers](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/stackalloc-array-initializers.md): Stack-allocated arrays can now be initialized, e.g. `Span<int> x = stackalloc[] { 1, 2, 3 };`.
- [Indexing movable fixed buffers](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/indexing-movable-fixed-fields.md): Fixed buffers can be indexed into without first being pinned.
- [Custom `fixed` statement](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/pattern-based-fixed.md): Types that implement a suitable `GetPinnableReference` can be used in a `fixed` statement.
- [Improved overload candidates](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/improved-overload-candidates.md): Some overload resolution candidates can be ruled out early, thus reducing ambiguities.
- [Expression variables in initializers and queries](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/expression-variables-in-initializers.md): Expression variables like `out var` and pattern variables are allowed in field initializers, constructor initializers and LINQ queries.
-	[Tuple comparison](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/tuple-equality.md): Tuples can now be compared with `==` and `!=`.
-	[Attributes on backing fields](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.3/auto-prop-field-attrs.md): Allows `[field: …]` attributes on an auto-implemented property to target its backing field.


## Visual Studio 2017 Version 15.6

The C# compiler now supports:
* Compiler server on CoreCLR, for build throughput performance
* Strong name signing on CoreCLR ([`/keyfile` option](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/keyfile-compiler-option), all OSes)

Two minor languages changes where made to the 7.2 language features:
* Tie-breaker for `in` overloads ([details](https://github.com/dotnet/csharplang/issues/945))
* Relax ordering of `ref` and `this` in ref extension methods ([details](https://github.com/dotnet/csharplang/issues/1022))

[Shipped APIs](TODO), [Bug fixes](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+milestone%3A15.6+is%3Aclosed)
 
## [Visual Studio 2017 Version 15.5](https://github.com/dotnet/roslyn/releases/tag/Visual-Studio-2017-Version-15.5)

The C# compiler now supports the 7.2 set of language features including:

* Support for the `Span<T>` type being used throughout Kestrel and CoreFX via the `ref struct` modifier.
* `readonly struct` modifier: Enforces that all members of a struct are `readonly`. This adds a layer of correctness to code and also allows the compiler to avoid unnecessary copying of values when accessing members. 
* `in` parameters / `ref readonly` returns: Allows for unmodifiable structs to be safely passed and returned with the same efficiency as modifiable `ref` values.
* `private protected` access modifier: Restricts access to the intersection of `protected` and `internal`.
* Non-trailing named arguments: Named arguments can now be used in the middle of an argument list without the requirement that all following arguments are passed by name as well. 

[Bug fixes](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+milestone%3A15.5+is%3Aclosed)
 
## [Visual Studio 2017 Version 15.3](https://github.com/dotnet/roslyn/releases/tag/Visual-Studio-2017-Version-15.3)

The C# compiler now supports the 7.1 set of language features, including:
- [Async Main methods](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.1/async-main.md)
- ["default" literals](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.1/target-typed-default.md)
- [Inferred tuple element names](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.1/infer-tuple-names.md)
- [Pattern-matching with generics](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.1/generics-pattern-match.md)

The C# and VB compilers now can produce [reference assemblies](https://github.com/dotnet/roslyn/blob/main/docs/features/refout.md).

When you use C# 7.1 features in your project, lightbulb offers to upgrade your project’s language version, to “C# 7.1” or “latest”.

[Shipped APIs](https://github.com/dotnet/roslyn/commit/5520eaccd5d22ae98a39a5f88120277f02097dbf), [Bug fixes](https://github.com/dotnet/roslyn/pulls?q=is%3Apr+milestone%3A15.3+is%3Aclosed)
 
 ## [Visual Studio 2017 Version 15.0](https://github.com/dotnet/roslyn/releases/tag/Visual-Studio-2017)
 The C# compiler now supports the [7.0](https://blogs.msdn.microsoft.com/dotnet/2017/03/09/new-features-in-c-7-0/) set of language features, including:
- [Out variables](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.0/out-var.md)
- [Pattern matching](https://github.com/dotnet/csharplang/blob/main/proposals/patterns.md)
- [Tuples](https://github.com/dotnet/roslyn/blob/main/docs/features/tuples.md)
- [Deconstruction](https://github.com/dotnet/roslyn/blob/main/docs/features/deconstruction.md)
- [Discards](https://github.com/dotnet/roslyn/blob/main/docs/features/discards.md)
- [Local Functions](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.0/local-functions.md)
- [Binary Literals](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.0/binary-literals.md)
- [Digit Separators](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.0/digit-separators.md)
- Ref returns and locals
- [Generalized async return types](https://github.com/dotnet/roslyn/blob/main/docs/features/task-types.md)
- More expression-bodied members
- [Throw expressions](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-7.0/throw-expression.md)
