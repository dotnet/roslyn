C# Design Meeting Notes for Feb 4, 2015
========================================

Discussion on these notes can be found at https://github.com/dotnet/roslyn/issues/396.

Agenda
------

1. Internal Implementation Only (C# 6 investigation)
2. Tuples, records and deconstruction
3. Classes with value semantics


1. Internal implementation only
===============================

We have a versioning problem in the Roslyn APIs that we suspect is somewhat common - that of inherited hierarchies.

The essence of the problem is that we want to describe an inheritance *hierarchy* of types in the abstract, as well as several concrete "implementations" of the hierarchy.

In Roslyn, the main example is that we have a language agnostic hierarchy of symbols, as well as a C# and VB specific implementation of that.

This leads to a need for multiple inheritance: "C#-specific local symbol" needs to both inherit "C#-specific symbol" and "abstract local symbol". The only way to represent this is to express the abstract hierarchy with interfaces:

``` c#
public interface ISymbol { ... }
public interface ILocalSymbol : ISymbol { ... }

public abstract class CSharpSymbol : ISymbol { ... }
public class CSharpLocalSymbol : CSharpSymbol, ILocalSymbol { ... }

// Etc...
```

So far, so good. However, even though `ISymbol` and friends are interfaces, we don't actually want anyone else to implement them. They are not intended as extension points, we merely made them interfaces to allow multiple base types. On the contrary we would like to be able to add members to these types in the future, as the Roslyn API evolves.

Had these types been abstract classes, we could have prevented implementation from other assemblies by having only internal constructors. That would have protected the types for future extensions. However, for interfaces no such trick exists.

As a result, if we ever want to evolve these interfaces by adding more members, but someone implemented them *despite our intent*, we will break those people.

Our options, current and future, seem to include:

1. Tell people really loudly in comments on the interfaces that they shouldn't implement them, because we will break them
2. Add an attribute, `ImplicitImplementationOnlyAttribute`, to the interfaces
3. Write an analyzer that enforces the attribute, make it be installed and on by default and hope that catches enough cases
4. Make the compiler enforce the attribute
5. Add a mechanism to the language to safely evolve interfaces
    * default implementations of interface methods (like in Java) - will require CLR support
6. Add an alternative multiple-inheritance mechanism to the language
    * mixins or traits

Options 1, 2 and 3 are open to us today. However, the concern is whether we can consider them strong enough discouragement that our future selves will feel good about evolving the interfaces. If we do 1, 2 and 3, will we then be ready to later break people who disregard the discouragement?

We have never been 100% on compat. Reliance on reflection, codegen patterns etc, can cause code to be breakable today. The BCL has a clear set of guidelines of which things they allow themselves to change even if they can break existing code: adding private fields, etc. 

Their rule on interfaces is that we cannot add members to an interface. In inherited-hierarchies situations such as the one in Roslyn that constraint is hard to live with.

Analyzers
---------

Analyzers can easily detect when an interface with the attribute is being implemented outside of its assembly.

The problem here is that it is easy to land in a situation where you do not have the analyzer, or it is not turned on. Analyzers are optional by design: they help you use an API, but the API author can't rely on them being enforced.

Compiler enforcement
--------------------

Moving this check into the compiler would make the "hole" smaller, but, perhaps surprisingly, wouldn't completely deal with it: a pre C# 6 compiler would still happily compile an implementation of an interface with the attribute on. Now, upgrading to C# 6 and the Roslyn-based compiler would be a breaking change!

Also, this may play badly with mocking. Though many of the automated mocking frameworks already have a way to work around internal requirements, explicit or manual mocking would still suffer.

There are probably some design details around how this works in conjunction with `InternalsVisibleTo`:

Assembly A:

``` c#
[assembly: InternalVisibleTo("B")]

[InternalImplementationOnly]
public interface IA {}
```

Assembly B:

``` c#
public abstract class B : IA {}
public interface IB : IA {} // implicitly inherits restriction?
```

Assembly C:

``` c#
public class C : B {}     // OK
public class D : B, IA {} // not OK
public class E : IB {}    // not OK
```

Conclusion 
----------

We need to talk to the BCL team to decide whether we would consider either of these approaches sufficient to protect the evolvability of interfaces.



2. Tuples, records, deconstruction
==================================

We are eager to look at language support for tuples. They should facilitate multiple return values from methods, including async ones, and a consumption experience that includes deconstruction.

We'll have a proposal ready to discuss in the next meeting (It is now here: #347).

We are also interested in making it much easier to write something akin to algebraic datatypes (discriminated unions), whose shape and contents are easily pattern-matched on and, probably, deconstructed.

While we have a proposal for records that caters to that (#206), there's a sense that this might be more closely connected with the tuple feature than the current proposals suggest, and we should churn on these together to produce a unified, or at least rationalized, design. 

Are tuples just a specific kind of record? Are records just tuples with a name? Further exploration is needed! 

The next point is one such exploration.


3. Classes with values
======================

We explored a possible pattern for immutable classes. It may or may not be the right thing to do, but it generated a lot of ideas for features that would be possible with it, and that it would be interesting to pursue regardless of the pattern.

The core idea is: if you want value semantics for your object, maybe your object should literally have a `Value`:

``` c#
public class Person 
{
	public readonly PersonValue Value;
	public Person(PersonValue value) { Value = value; }
	public string Name => Value.Name;
	public int Age => Value.Age;
	…
	public struct PersonValue
	{
		public string Name;
		public int Age;
	}
}
```

The core idea is that there is a *mutable* value type representing the actual state. The object wraps a `readonly` field `Value` of that value type and exposes properties that just delegate to the `Value`.

This starts out with the obvious downside that there are two types instead of one. We'll get back to that later.


Value semantics
---------------

Value semantics mean a) being immutable and b) having value-based equality (and hash code). Value types *already* have value-based equality and hash code, and the `Value` field is `readonly`. So instead of having to negotiate computation of hashcodes, a developer using this pattern can just forward these questions to the `Value`:

``` c#
public override bool Equals(object other) => (other as Person)?.Value.Equals(Value) ?? false; // or similar
public override int GetHashCode() => Value.GetHashCode;
```

No need to write per-member code. All is taken care of by the `Value`. So for bigger types this definitely leads to less code bloat.

You could implement `IEquatable<T>` for better performance, but this illustrates the point.


Builder pattern
---------------

This approach is essentially a version of the Builder pattern: `PersonValue` acts as a builder for `Person` in that it can be manipulated, and eventually passed to the constructor (which implements the Parameter Object pattern by taking all its parameters as one object).

This allows for the use of object initializers in creating new instances:

``` c#
var person = new Person(new PersonValue { Name = "John Doe" });
```

It also lets you create new objects from old once using mutation to incrementally change it:

``` c#
var builder = Person.Value;
builder.Name = "John Deere";
person = new Person(builder);
```

This again scales to much larger objects, and illustrates one of the points why the builder pattern is popular. Another popular approach is to use "Withers", methods for returning a new instance of an immutable type that's incrementally changed in one way from an old one:

``` c#
person.WithName("John Deere");
```

Withers are more elegant to use, at least when you are only changing one property. But when changing multiple ones, you need to chain them, creating intermediate objects along the way. What's worse, you need to declare a `WithFoo` method for every single property, which is a lot of boilerplate.

Maybe we can make the Builder pattern more elegant to use - more like the Wither pattern?


Object initializers
-------------------

One idea is to allow two new uses of object initializers:

* when creating new immutable objects that implement the builder pattern
* when non-destructively "modifying" immutable objects by creating new ones with deltas

First, let's allow this (similar to what's proposed in #229):

``` c#
var person = new Person { Name = "John Doe" };
```

It would simply rewrite to this:

``` c#
var person = new Person(new PersonValue { Name = "John Doe" });
```

Which is what we had above.

Second, let's allow object initializers on *existing* values instead of just new expressions. This is a much requested feature already, because it would apply to factories etc.

It would allow us to do a lot better on the incremental modification:

``` c#
person = new Person(person.Value { Name = "John Deere" });
```

If we are uncomfortable just letting object initializers occur directly on any expression, we could consider adding a keyword, like `with`.

Now we can get the builder, modify it and create a new Person from it, all in one expression.

But of course the kicker is to combine the two ideas, allowing you to write:

``` c#
person = person { Name = "John Deere" };
```

to the same effect: 

* Since `person` doesn't have a writable member `Name`, get its builder
* modify the builder based on the object initializer
* create a new `Person` from the builder

So in summary, adding the builder patter in *some* compiler-recognized form allows us to tinker with the object initializer feature, making it work as well for immutable objects as  it does for mutable ones, and essentially serving as built-in "wither" support.


Using tuples as the values
--------------------------

Tuples are far from a done deal, but assuming a design like #347, where tuples are mutable structs, they could actually serve as the type of the `Value` field, thus obliterating the need for a second type declaration:

``` c#
public class Person 
{
	public readonly (string Name, int Age) Value; // a tuple
	public Person((string Name, int Age) value) { Value = value; }
	…
}
```

This opens up one more opportunity. Assuming you want your immutable type to be deconstructable the same way as a tuple - by position. How do you indicate that? Well maybe having a tuple as your `Value` is how: deconstruction is simply yet another aspect that is delegated to the `Value`:

``` c#
(var n, var a) = GetPerson(); // deconstructed as per the `Value` tuple
```

Records
-------

If we have a pattern that the compiler supports in various ways for immutable types (whether this pattern or another), it makes sense that that pattern should also underlie any record syntax that we add.

Assuming a record syntax like in #206, the record definition:

``` c#
class Person(string Name, int Age);
```

Would then generate the class

``` c#
class Person 
{
    public readonly (string Name, int Age) Value;
	public Person((string Name, int Age) value) { Value = value; }
    public string Name => Value.Name;
    public int Age => Value.Age;
    public override bool Equals(object other) => (other as Person)?.Value.Equals(Value) ?? false; // or similar
    public override int GetHashCode() => Value.GetHashCode;
}
```

And would therefore support object initializers and positional deconstruction.


Conclusion
----------

We are not at all sure if this is the right general direction, let alone the right *specific* pattern if it is. But builders are a common pattern, and for a reason. It would be interesting if support for them was somehow included in what we do for immutable types in C# 7.
