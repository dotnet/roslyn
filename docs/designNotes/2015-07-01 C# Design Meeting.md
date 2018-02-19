# C# Design Meeting Notes for Jul 1, 2015

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/3913.

## Agenda

We are gearing up to prototype the tuple feature, and put some stakes in the ground for its initial design. This doesn't mean that the final design will be this way, and some choices are arbitrary. We merely want to get to where a prototype can be shared with a broader group of C# users, so that we can gather feedback and learn from it.

# Tuples

The tuples proposal #347 is pretty close to what we want, but has some open questions and unaddressed aspects.

## Names

We want the elements of tuples to be able to have names. It is exceedingly useful to be able to describe which elements have which meaning, and to be able to dot into a tuple with those names.

However, it is probably not useful to make the names too strongly a part of the type of a tuple. There is no really good reason to consider (int x, int y) an fundamentally different tuple type than (int a, int b). In fact, according to the analogy with parameter lists, the names should be of secondary importance: useful for getting at the elements, yes, but just as parameter lists with different parameter names can overwrite each other, as long as the types match at the given parameter positions, so should tuple types be considered equivalent when they have the same types at the same positions.

Another way to view it is that all tuples with the same types at the same positions share a common underlying type. We will make that type denotable in the language, in that you can write anonymous tuple types, `(int, int)`, even though you cannot write an anonymous parameter list.

For now we don't think we will allow partially named tuples: it is either all names or none.

``` c#
(int x, int y) t1 = ...;
(int a, int b) t2 = t1; // identity conversion
(int, int) t3 = t1;     // identity conversion
```

All "namings" of a tuple type are considered equivalent, and are convertible to each other via an identity conversion. For type inference purposes, an inferred tuple type will have the names if all "candidate types" with names agree on them, otherwise it will be unnamed.

``` c#
var a1 = new[]{ t1, t1 }; // infers (int x, int y)[], since all agree
var a2 = new[]{ t1, t2 }; // infers (int, int)[], since not all agree
var a3 = new[]{ t1, t3 }; // infers (int x, int y)[] since all with names agree
```

For method overriding purposes, a tuple type in a parameter or return position can override a differently named tuple type. The rule for which names apply at the call site are the same as those used for named arguments: the names from the most specific statically known overload.

Tuple literals likewise come in named an unnamed versions. They can be target typed, but sometimes have a type of their own:

``` c#
var t1 = ("Hello", "World");           // infers (string, string)
var t2 = (first: "John", last: "Doe"); // infers (string first, string last)
var t3 = ("Hello", null);              // fails to infer because null doesn't have a type
var t4 = (first: "John", last: null);  // fails to infer because null doesn't have a type

(string, string) t5 = ("Hello", null);                           // target typed to (string, string)
(string first, string last) t6 = ("John", null);                 // target typed to (string first, string last)
(string first, string last) t7 = (first: "John", second: "Doe"); // error: when given, names must match up
(string first, string last) t8 = (last: "Doe", first: "John");   // fine, values assigned according to name
```

The last two are probably the only possibly controversial examples. When target typing with names in the literal, this seems very similar to using named arguments for a method call. These rules match that most closely.

This is something we may want to return to, as it has some strange consequences. For instance, if we introduce a temporary variable for the tuple, and do not use target typing:

``` c#
var tmp = (last: "Doe", first: "John"); // infers (string last, string first)
(string first, string last) t8 = tmp;   // assigns by position, so first = "Doe"
```

But for now, let's try these rules out and see what they feel like.

## Encoding

Are core question is what IL we generate for tuples. The equivalence-across-names semantics make it easy to rely on a fixed set of underlying generic library types.

We want tuples to be value types, since we expect them to be created often and copied less often, so it's probably worthwhile avoiding the GC overhead.

Strangely perhaps, we want tuples to be mutable. We think that there are valid scenarios where you want to keep some data in a tuple, but be able to modify parts of it independently without overwriting the whole tuple. Also, calling methods on readonly tuple members that are themselves value types, would cause those methods to be called on a throw-away copy. This is way too expensive - it means that there may be no non-copying way of doing e.g. value-based equality on such tuples.

So we encode each tuple arity as a generic struct with mutable fields. It would be very similar to the current Tuple<...> types in the BCL, and we would actively avoid purely accidental differences. For this reason, the fields will be called `Item1` etc. Also, tuples bigger than a certain arity (probably 8)  will be encoded using nesting through a field called `Rest`.

So we are looking at types like this:

``` c#
public struct ValueTuple<T1, T2, T3>
{
    public T1 Item1;
    public T2 Item2;
    public T3 Item3;
}
```

Possibly with a constructor, some `Equals` and `GetHashCode` overrides and maybe an implementation of `IEquatable<...>` and `IStructurallyEquatable<...>`, but at its core exceedingly simple.

In metadata, a named tuple is represented with its corresponding `ValueTuple<...>` type, plus an attribute describing the names for each position. The attribute needs to be designed to also be able to represent names in nested tuple types.

The encoding means that an earlier version of C#, as well as other languages, will see the tuple members as `Item1` etc. In order to avoid breaking changes, we should probably keep recognizing those names as an alternative way of getting at any tuple's elements. To avoid confusion we should disallow `Item1` etc. as names in named tuples - except, perhaps, if they occur in their right position.

## Deconstruction syntax

Most tuple features have a deconstruction syntax along the following lines:

``` c#
(int sum, int count) = Tally(...);   
```

If we want such a syntax there are a number of questions to ask, such as can you deconstruct into existing fields or only newly declared ones?

For now we sidestep the question by not adding a deconstruction syntax. The names make access much easier. If it turns out to be painful, we can reconsider.

## Other issues

We will not consider tuples of arity zero or one - at least for now. They may be useful, especially from the perspective of generated source code, but they also come with a lot of questions.

It seems reasonable to consider other conversions, for instance implicit covariant conversions between tuples, but this too we will let lie for now.  
