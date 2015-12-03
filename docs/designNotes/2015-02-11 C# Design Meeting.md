C# Design Meeting Notes for Feb 11, 2015
=========================================

Discussion on these notes can be found at https://github.com/dotnet/roslyn/issues/1207.

Agenda
------

1. Destructible types <*we recognize the problem but do not think this is quite the right solution for C#*>
2. Tuples <*we like the proposal, but there are several things to iron out*>



1. Destructible types
=====================

Issue #161 is a detailed proposal to add destructible types to C#.

The problem area is deterministic disposal of resources. 

Finalizers are notoriously non-deterministic (you can never know when or even whether they run), and rampant usage is quite a hit on performance. `using` statements are the language's current attempt at providing for deterministic disposal.

The problem with `using` statements is that no-one is forced to use them. An `IDisposable` resource is disposed of only if people remember to wrap it in a `using`. FxCop rules and now Roslyn diagnostics can be employed to help spot places where things aren't disposed that should be, but these have notoriously run into false positives and negatives, to the extent that they are a real nuisance and often get turned off.

At least one reason for this is the lack of their ability to track ownership. Whose responsibility is it to ultimately dispose the resource, and how is that responsibility transferred?

Destructible types offer a sweeping approach to deal with this, that is a complete departure from `using` and `IDisposable` and instead leans more closely on C++-like RAII features.

The core idea is that destructible types are a language concept (they have to be declared as such):

``` c#
destructible struct Handle
{
    IntPtr ptr;

    ~Handle()
    {
        if (ptr != IntPtr.Zero) Free(ptr);
    }
}
```

Destructible types can only be fields in other destructible types.

Variables of destructible type have their contents automatically disposed when they go out of scope.

To prevent multiple disposal, such variables can't just be assigned to others. Instead, ownership must be explicitly transferred (with a `move` keyword):

``` c#
void M()
{
    Handle h = CreateHandle();
    Handle h2 = h; // Not allowed! Moves are explicit
    Handle h3 = move h; // Explicit owner transfer
    
    M2(new Handle()); // ok, no owner
    M2(h3); // Not allowed!
    M2(move h3); // ok, explicit owner transfer

}
void M2(Handle h) ...
```

At the end of M, all locals with destructible types are destructed, in opposite order of their introduction.

Additionally variables can be "borrowed" by being passed by `ref` to a method.

There are a lot of limitations to the feature, and it doesn't replace `IDisposable`, because you cannot use destructible types as type arguments, in arrays, and as fields of non-destructible types. Also, they cannot implement interfaces.

As a release valve one can have a `Box<T>` that "magically" transfers the ownership of a destructible type to the finalizer. The finalizer will crash the process if the destructible hasn't been properly destructed.

There has been some negative feedback around the destruction being implicit, even as the ownership transfer was explicit. When assignment overwrites a value, that value has to be destructed. But what about cases like this?

``` c#
{
    D x;
    if (e) x = new x();
    x = new x();
}
```

How to know whether the second assignment overwrites a value, that should therefore be destructed? We'd have to keep track at runtime. Or prevent this situation through definitely assigned or unassigned rules, or the like.


Conclusion
----------

Overall, we see how this adds value, but not enough that having these concepts in the face of all C# developers. It doesn't earn back it's -100 points.

Could we take a step back and have analyzers encourage practices that are less error prone? Yes. You just cannot have analyzers enforce correctness. And you cannot have them improve perf by affecting codegen. And they cannot do the heavy lifting for you.

An analyzer can help you with where to put your `using` statements. FxCop tries to do that today, but has too many false positives (use vs ownership). Even if you could get rid of those, there's a lot of code you'd need to write.

People do struggle with `IDisposable` today. They don't know when things are `IDisposable`, whether they are the ones who should dispose things.

We would like to solve the problem in C#, and pick over this proposal for ideas, but we'd need to noodle with it to make it a better fit. It would need to integrate better with current `IDisposable`.



2. Tuples
=========

Issue #347 is a proposal for adding tuples to C#. The main guiding principle for the proposal is to enable multiple return values, and wherever possible design them in analogy with parameter lists.

As such, a tuple type is a parenthesized list of types and names, just like a parameter list:

``` c#
public (int sum, int count) Tally(IEnumerable<int> values) { ... }

var t = Tally(myValues);
Console.WriteLine($"Sum: {t.sum}, count: {t.count}");  
```

Along the same lines, tuple values can be constructed using a syntax similar to argument lists. By position:

``` c#
public (int sum, int count) Tally(IEnumerable<int> values) 
{
    var sum = 0; var count = 0;
    foreach (var value in values) { sum += value; count++; }
    return (sum, count); // target typed to the return type
}
```

Or by name:

``` c#
public (int sum, int count) Tally(IEnumerable<int> values) 
{
    var res = (sum: 0, count: 0); // infer tuple type from names and values
    foreach (var value in values) { res.sum += value; res.count++; }
    return res;
}
```

Finally, there'd be syntax to deconstruct tuples into variables for the individual members:

``` c#
(var sum, var count) = Tally(myValues); // deconstruct result
Console.WriteLine($"Sum: {sum}, count: {count}");  
```

Tuples should be thought of as temporary, ephemeral constellations of values without inherent conceptual connection: they just "happen to be traveling together".

We are generally eager to pursue this design. There are a number of open issues, that we explore in the following.


Type equivalence
----------------

Intuitively you'd expect that every time you use the same tuple type expression, it denotes the same type - that there is *structural equivalence*. However, depending on how we implement tuple types on top of the CLR, that may not always be easy to achieve - especially when unifying across different assemblies.

To take it to an extreme, what if they didn't even unify *within* an assembly? Even then they'd probably have significant value. They would be good for the "ephemeral" core scenario of things traveling together. It wouldn't let them serve as e.g. builders; they would really only work for the multiple return values scenario.

Even then there'd be issues with virtual method overrides, etc: When you override and restate the return type, it doesn't really work if that declares a *different* return type.

``` c#
class C
{
    public abstract (int x, int y) GetCoordinates();
}
class D : C
{
    public override (int x, int y) GetCoordinates() { ... } // Error! different return type!!?!
}
```

A middle ground is for these methods to unify *within* but not *across* assemblies. This is what we do for anonymous types today. However, there we are careful never to let the types occur in member signatures, whereas that would be the whole point of tuples.

This makes a little more sense than no unification at all, since at least all mentions of a tuple type within the same member body and even type body refer to the same type. However there'd still be weird behavior involving overrides across assembly boundaries, equivalence of generic types constructed with tuple types, etc. We could try to be smart about reusing types from other assemblies when possible, etc, but it would be quite a game of whack-a-mole to get all the edge cases behaving sensibly.

The only thing that really makes sense at the language level is true structural equivalence. Unfortunately, the CLR doesn't really provide for unification of types with similar member names across assemblies. Ideally we could *add* that to the CLR, but adding new functionality to the CLR is not something to be done lightly, as it breaks all downlevel targeting.

A way to get structural equivalence working at compile time would be to encode tuples with framework types similar to the current `Tuple<...>` types. Member access would be compiled into access of members called `Item1` etc. The language level member names would be encoded in attributes for cross-assembly purposes:

``` c#
struct VTuple2<T1, T2> { ... }
struct VTuple3<T1, T2, T3> { ... }

/* Source: */
(x: int, y: int) f() { ... }

/* Generates: */
[TupleNames("x", "y")]
VTuple2<int, int> f() {...}
```

Unfortunately this leaves problems at runtime: now there's *too much* type equivalence. If I want to do a runtime test to see if an object is an `(int x, int y)`, I would get false positives for an `(int a, int b)`, since the names wouldn't be part of the runtime type:

``` c#
object o = (a: 7, b: 9);
if (o is (int x, int y) t) WriteLine(t.x); // false positive
```

Also the member names would be lost to `dynamic`:

``` c#
dynamic d = (a: 7, b: 9);
WriteLine(t.a); // Error! Huh ??!?
```


Of course we could come up with tricks so that a tuple would carry some kind of member name info around at runtime, but not without cost. Or we could decide that the names are ephemeral, compile time only entities, kind of like parameter names, and losing them when boxing is actually a good thing. 

Also, this approach means that languages that don't know about tuples would see the actual `Item1` member names when referencing member signatures containing tuples. Such languages include previous versions of C#! So for compatibility with those versions of C#, even in the new, tuple-aware, version, access through `Item1` etc would have to be still legal (though probably hidden from IntelliSense etc.).

A nice thing about this scheme is that it leads to very little code bloat: it only relies on framework types, and not on compiler generated types in the assembly.

This remains an issue for further debate.


Conversions
-----------

There are several kinds of conversions you can imagine allowing between tuple types:

``` c#
(string name, int age) t = ("Johnny", 29);

/* Covariance */
(object name, int age) t1 = t;

/* Truncation */
(object name) t2 = t;

/* Reordering */
(int age, string name) t3 = t;

/* Renaming */
(string n, int a) t4 = t;
```

Note that you cannot have both reordering and renaming in the language; we would have to choose.

However, we are more inclined not to allow *any* of these conversions. They don't seem particularly helpful, and are likely to mask something you did wrong. People can always deconstruct and reconstruct if they want to get to a different tuple type, so our default position is to be super rigid here and only match exactly the same names and types in exactly the same order.


Deconstruction
--------------

There are a couple of issues with the proposed deconstruction syntax.

First of all, since the recipient variables are on the left, there is no equivalent to parameter help guiding you as to what to name them. In that sense it would be better to have a syntax that receives the values on the right. But that seems an odd place to declare new variables that are intended to be in scope throughout the current block:

``` c#
Tally(myValues) ~> (var sum, var count); // strawman right side alternative
Console.WriteLine($"Sum: {sum}, count: {count}");  
```

That's not an actual proposed syntax, but just intended to show the point!

A related issue is that you'd sometimes want to deconstruct into *existing* variables, not declare new ones:

``` c#
(sum, count) = Tally(myValues);
```

Should this be allowed? Is deconstruction then a declaration statement, an assignment statement or something different?

Finally, once we have a deconstruction syntax, we'd probably want to enable it for other things than tuples. Maybe types can declare how they are to be deconstructed, in a way that the language understands. So deconstruction syntax should be considered in this broader context. For instance, if deconstructable types can be reference types, can deconstruction throw a null reference exception?


LINQ
----

You could imagine tuples becoming quite popular in LINQ queries, to the point of competing with anonymous types:

``` c#
from c in customers
select (name: c.Name, age: c.Age) into p
where p.name == "Kevin"
select p;
``` 

To this end it would be useful for tuple "literals" to support *projection*, i.e. inference of member names, like anonymous types do:

``` c#
from c in customers
select (c.Name, c.Age) into p // infers (string Name, int Age)
where p.Name == "Kevin"
select p;
```


Out parameters
--------------

F# allows out parameters to be seen as additional return values. We could consider something similar:

``` c#
bool TryGet(out int value){ ... }

/* current style */
int value;
bool b = TryGet(out value);

/* New style */
(int value, bool b) = TryGet();
```


Conclusion
----------

We'd very much like to support tuples! There are a number of open questions, as well as interactions with other potential features. We'll want to keep debating and experimenting for a while before we lock down a design, to make sure we have the best overall story.