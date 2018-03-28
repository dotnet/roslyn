C# Design Meeting Notes for Apr 15
==================================

Discussion thread for these notes is at https://github.com/dotnet/roslyn/issues/2133.

Agenda
------

In this meeting we looked at nullability and generics. So far we have more challenges than solutions, and while we visited some of them, we don't have an overall approach worked out yet.

1. Unconstrained generics
2. Overriding annotations
3. FirstOrDefault
4. TryGet


Unconstrained generics
======================

Fitting generics and nullability together is a delicate business. Let's look at unconstrained type parameters as they are today. One the one hand they allow access to members that are on all objects - `ToString()` and so on - which is bad for nullable type arguments. On the other hand they allow `default(T)` which is bad for non-nullable type arguments.

Nevertheless it seems draconian to not allow instantiations of unconstrained type parameters with, say, `string?` and `string!`. We suspect that most generic types and methods probably behave pretty nicely in practice.

For instance, `List<T>`, `Dictionary<T>` etc. would certainly have internal  data structures - arrays - that would have the default value in several places. However, their logic would be written so that no array element that hadn't explicitly been assigned a value from the user would ever expose its default value later. So if we look at a `List<string!>` we wouldn't actually ever see nulls come out as a result of the internal array having leftover nulls from its initial allocation. No nulls would come in, and therefore no nulls would come out. We want to allow this.


Overriding annotations
======================

When type parameters are known to be reference types it may make sense to allow overriding of the nullability of the type parameter: `T?` would mean `sting?` regardless of whether the type argument was `string!`, `string` or `string?`.

This would probably help in some scenarios, but with most generics the type parameter isn't actually known to be a reference type.


FirstOrDefault
==============

This pattern explicitly returns a default value if the operation fails to find an actual value to return. Obviously that is unfortunate if the element type is non-nullable - you'd still get a null out!

In practice, such methods tend to be so glaringly named (precisely because their behavior is a little funky) that most callers would probably already be wary of the danger. However, we might be able to do a little better.

What if there was some annotation you could put on `T` to get the nullable version *if* `T` should happen to be a reference type. Maybe the situation is rare enough for this to just be an attribute, say `[NullableIfReference]`:

``` c#
public [return:NullableIfReference] T FirstOrDefault<T>(this IEnumerable<T> src)
{
    if ...
    else return default(T);
}
```

Applied to `List<string!>` (or `List<string>`) this would return a `string?`. But applied to `List<int>` it would return an `int` not an `int?`.

This seems perfectly doable, but may not be worth the added complexity.


TryGet
======

This pattern has a bool return value signaling whether a value was there, and an out parameter for the result, which is explicitly supposed to be default(T) when there was no value to get:

``` c#
public bool TryGet(out T result) { ... }
```

Of course no-one is expected to ever look at the out parameter if the method returns false, but even so it might be nice to do something about it.

This is not a situation where we want to apply the `[NullableIfReference]` attribute from above. The consumer wants to be able to access the result without checking for null if they have already checked the returned bool!

We could imagine another attribute, `[NullableIfFalse]` that would tell the compiler at the consuming site to track nullability based on what was returned from the method, just as if there had been a null check directly in the code.

Again, this might not be worth the trouble but is probably doable.
