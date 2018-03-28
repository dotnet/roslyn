# C# Language Design Review, Apr 22, 2015

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/3910.

## Agenda

See #1921 for an explanation of design reviews and how they differ from design meetings.

1. Expression tree extension
2. Nullable reference types
3. Facilitating wire formats
4. Bucketing


# Expression Trees

Expression trees are currently lagging behind the languages in terms of expressiveness. A full scale upgrade seems like an incredibly big investment, and doesn't seem worth the effort. For instance, implementing `dynamic` and `async` faithfully in expression trees would be daunting.

However, supporting `?.` and string interpolation seems doable even without introducing new kinds of nodes in the expression tree library. We should consider making this work.


# Nullable reference types

A big question facing us is the "two-type" versus the "three-type" approach: We want you to guard member access etc. behind null checks when values are meant to be null, and to prevent you from sticking or leaving null in variables that are not meant to be null. In the "three-type" approach, both "meant to be null" and "not meant to be null" are expressed as new type annotations (`T?` and `T!` respectively) and the existing syntax (`T`) takes on a legacy "unsafe" status. This is great for compatibility, but means that the existing syntax is unhelpful, and you'd only get full benefit of the nullability checking by completely rooting out its use and putting annotations everywhere.

The "two-type" approach still adds "meant to be null" annotations (`T?`), but holds that since you can now express when things *are* meant to be null, you should only use the existing syntax (`T`) when things are *not* meant to be null. This certainly leads to a simpler end result, and also means that you get full benefit of the feature immediately in the form of warnings on all existing unsafe null behavior! Therein of course also lies the problem with the "two-type" approach: in its simplest form it changes the meaning of unannotated `T` in a massively breaking way.

We think that the "three-type" approach is not very helpful, leads to massively rewritten over-adorned code, and is essentially not viable. The "two-type" approach seems desirable if there is an explicit step to opt in to the enforcement of "not meant to be null" on ordinary reference types. You can continue to use C# as it is, and you can even start to add `?` to types to force null checks. Then when you feel ready you can switch on the additional checks to prevent null from making it into reference types without '?'. This may lead to warnings that you can then either fix by adding further `?`s or by putting non-null values into the given variable, depending on your intent.

There are additional compatibility questions around evolution of libraries, but those are somewhat orthogonal: Maybe a library carries an assembly-level attribute saying it has "opted in", and that its unannotated types should be considered non-null.

There are still open design questions around generics and library compat.


# Wire formats

We should focus attention on making it easier to work with wire formats such as JSON, and in particular on how to support strongly typed logic over them without forcing them to be deserialized to strongly typed objects at runtime. Such deserialization is brittle, lossy and clunky as formats evolve out of sync, and extra members e.g. aren't kept and reserialized on the other end.

There's a range of directions we could take here. Assuming there are dictionary-style objects representing the JSON (or other wire data) in a weakly typed way, options include:

* Somehow supporting runtime conversions from such dictionaries to interfaces (and back)
* Compile-time only "types" a la TypeScript, which translate member access etc. to a well-known dictionary pattern
* Compile-time type providers a la F#, that allow custom specification not only of the compile-time types but also the code generated for access.

We'd need to think about construction, not just consumption.

``` c#
var thing = new Thing { name = "...", price = 123.45 }
```

Maybe `Thing` is an interface with an attribute on it:

``` c#
[Json] interface { string name; double price; }
```

Or maybe it is something else. This warrants further exploration; the right feature design here could be an extremely valuable tool for developers talking to wire formats - and who isn't?

# Bucketing

We affirmed that the bucketing in issue #2136 reflects our priorities.