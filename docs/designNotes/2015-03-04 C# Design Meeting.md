C# Design Meeting Notes for Mar 4, 2015
========================================

Discussion on these notes can be found at https://github.com/dotnet/roslyn/issues/1303.

Agenda
------

1. `InternalImplementationOnly` attribute <*no*>
2. Should `var x = nameof(x)` work? <*no*>
3. Record types with serialization, data binding, etc. <*keep thinking*>
4. "If I had a [billion dollars](http://www.infoq.com/presentations/Null-References-The-Billion-Dollar-Mistake-Tony-Hoare)...": nullability <*daunting but worth pursuing*>



1. InternalImplementationOnly
=============================

This was a last-minute proposal for C# 6 and VB 14 to recognize and enforce an attribute to the effect of disallowing non-internal implementation of an interface. It would be useful to represent "abstract hierarchies", while reserving the right to add new members at a later stage.


Conclusion
---------- 

Let's not do this. We cannot enforce it well enough since old compilers don't know about it. 



2. Should `var x = nameof(x)` work?
===================================

This was raised as issue #766.

``` c#
var POST = nameof(POST);
```


Conclusion
----------

This works the same as with any other construct, i.e.: not. This is not a special case for `nameof`, and it doesn't seem worth special casing to allow it.



3. Records
==========

There's general excitement around proposals such as #206 to add easier syntax for declaring "records", i.e. simple data shapes.

This discussion is about the degree to which such a feature should accommodate interaction with databases, serialization and UI data binding. 

The problem has compounded in recent times. In a mobile app, pretty much everything has to be serialized one way or another. There's a big difference between e.g. JSON and binary serialization. In one you want readable property names (in a certain format), in the other you want compactness. ORMs are also essentially serialization. POCO was invented in that same context, but only gets you part of the way there.

Clearly, tying the language feature to specific such technologies is not a sustainable idea. There will not be built-in JSON literals, or `INotifyPropertyChanged` implementation or anything like that.

That said, It is worth thinking about what can be done *in general* to support such features better.

One problem is that each specific serialization and data binding technology tends to have certain requirements of the shape of the data objects. Serialization frameworks may expect the fields to be mutable, or to have certain attributes on them. UIs may expect properties to notify on change. Such requirements make it hard to have a single language-sanctioned mechanism for specifying the data shapes.

A separate problem may be the degree to which reflection is used by these technologies. It is commonly used to get at metadata such as member names, but is also sometimes used to set or get the actual values. It ends up being the case that both serialization and UI use the objects only in a "meta" fashion; the only part of the program that actually makes use of the data in a strongly typed way ends up being the business logic in between.

So in effect, data objects need to both facilitate strongly typed business logic based on their property names and shapes (and often in hierarchies), *and* satisfy requirements that are specific to the serialization and UI environments they are used in. Cross-cutting aspects, one might say, that clash in the implementation of the data objects.

There are several possible approaches, not necessarily mutually exclusive:

**Write the data types manually**: Often what ends up happening today. Just grit your teeth and churn out those INotifyPropertyChanged implementations.

**Separate objects**: Often the "best practice" is to have separate objects for serialization, logic, and presentation. But having to write the glue code between these is a big pain, and people often have to do at least some of it manually.

**Non-reflective meta-level access model**: E.g. implement dictionary-like behavior. Prevents need for reflection, but is it actually better or faster?

**"Fake" types**: Maybe at runtime the data objects shouldn't have strongly typed properties at all. Maybe the business logic is written up against a new kind of types that are only there at compile time, and which either simply erase like in TypeScript, or cause weakly typed but smart access code to be generated like with F# type providers. Then, from the perspective of serialization and UI, all the objects have the same type.

**Compile-time metaprogramming**: Generate the types (either into source or IL) from code describing the data shapes, adding whatever serialization or presentation functionality is required.

A particularly common requirement of these frameworks is for the data properties to be mutable. That might clash spectacularly with a records feature that tries to encourage immutability by default.


Conclusion
----------

We'll keep thinking about this situation. A next step in particular is to reach out to teams that own the serialization and presentation technologies that we'd like to work well with.



Nullability and reference types
===============================

The horrifying thing about null being allowed as a member of reference types is that it does not obey the contract of those types. Reference types can be null *and* reference types can be dereferenced, But dereferencing null is a runtime error! No wonder Sir Tony famously calls null pointers his "[billion dollar mistake](http://www.infoq.com/presentations/Null-References-The-Billion-Dollar-Mistake-Tony-Hoare)".

There are type systems that prevent null reference errors, and it is easy enough to do. It takes different shapes in different languages, but the core of it is that there is a non-nullable kind of reference type that can be dereferenced but cannot be null, and a nullable "wrapper" of that, which *can* be null, but cannot be dereferenced. The way you go from nullable to nonnullable is through a guard to test that the nullable value wasn't in fact null. Something like:

``` c#
string? n; nullable string
string! s; non-nullable string

n = null; // Sure; it's nullable
s = null; // Error!

WriteLine(s.Length); // Sure; it can't be null so no harm
WriteLine(n.Length); // Error!
if (n is string! ns) WriteLine(ns.Length); // Sure; you checked and dug out the value
```

Of course, if such a type system were around from day one, you wouldn't need the `!` annotation; `string` would just *mean* non-nullable.

It's a very common request (in fact [the top C# request on UserVoice](http://visualstudio.uservoice.com/forums/121579-visual-studio/category/30931-languages-c)) for us to do something about null reference exceptions along those lines. There are numerous obstacles, however:

**Backward compatibility**: The cat is already very much out of the bag in that existing code needs to continue to compile without error - no matter how null-unsafe it is. The best we can do about *existing* code is to offer optional analyzers to point out dangerous places.

**A default value for all types**: It is deeply ingrained in .NET and the CLR that every type has a default value. When you create an array with `new T[10]`, the array elements are initialized to the default value of `T`. There's even syntax to get the default value of a type, `default(T)`, and it is allowed even on unconstrained type parameters. But what is the default value of a non-nullable reference type?

**Definite assignment of non-nullable fields is unenforceable**. Eric Lippert explains it well [on his blog](http://blog.coverity.com/2013/11/20/c-non-nullable-reference-types/).

**Evolving libraries is breaking**: Say we added non-null annotations to the language. You'd then want to use them on your existing libraries to provide further guidance to your clients. Adding them on parameters would of course be breaking:

``` c#
public void Foo(string! name) { ... } // '!' newly added

Foo(GetString()); // Unless GetString also annotated, this is now broken
```

More subtly, adding the extra guarantee of non-nullability to a *return* type would also be breaking:

``` c#
public string! Foo() { ... } // '!' newly added

var s = Foo(); // Now infers 'string!' instead of 'string' for s
...
s = GetString(); // So this assignment is now broken
```

Type inference and overload resolution generally make strengthening of return types a potentially breaking change, and adding non-nullability is just a special case of that.


Conclusion
----------

Moving C# to a place of strong guarantees around nullability seems out of the question. But that is not to say that we cannot come up with an approach that meaningfully reduces the number of null reference exceptions. We want to keep digging here to see what can be done. The perfect is definitely the enemy of the good with this one.