C# Design Notes for Apr 6, 2016
===============================

Discussion for these design notes can be found at https://github.com/dotnet/roslyn/issues/10429.

We settled several open design questions concerning tuples and pattern matching.

# Tuples

## Identity conversion

Element names are immaterial to tuple conversions. Tuples with the same types in the same order are identity convertible to each other, regardless of the names.

That said, if you have an element name at *one* position on one side of a conversion, and the same name at *another* position on the other side, you almost certainly have bug in your code:

``` c#
(string first, string last) GetNames() { ... }
(string last, string first) names = GetNames(); // Oops!
```

To catch this glaring case, we'll have a warning. In the unlikely case that you meant to do this, you can easily silence it e.g. by assigning through a tuple without names at all.

## Boxing conversion

As structs, tuples naturally have a boxing conversion. Importantly, the names aren't part of the runtime representation of tuples, but are tracked only by the compiler. Thus, once you've "cast away" the names, you cannot recover them. In alignment with the identity conversions, a boxed tuple will unbox to any tuple type that has the same element types in the same order.

## Target typing

A tuple literal is "target typed" whenever possible. What that means is that the tuple literal has a "conversion from expression" to any tuple type, as long as the element expressions of the tuple literal have an implicit conversion to the element types of the tuple type.

``` c#
(string name, byte age) t = (null, 5); // Ok: the expressions null and 5 convert to string and byte
```

In cases where the tuple literal is not part of a conversion, it acquires its "natural type", which means a tuple type where the element types are the types of the constituent expressions. Since not all expressions have types, not all tuple literals have a natural type either:

``` c#
var t = ("John", 5); // Ok: the type of t is (string, int)
var t = (null, 5); //   Error: null doesn't have a type
```

A tuple literal may include names, in which case they become part of the natural type:

``` c#
var t = (name: "John", age: 5); // The type of t is (string name, int age)
```

## Conversion propagation

A harder question is whether tuple types should be convertible to each other based on conversions between their element types. Intuitively it seems that implicit and explicit conversions should just "bleed through" and compose to the tuples. This leads to a lot of complexity and hard questions, though. What kind of conversion is the tuple conversion? Different places in the language place different restrictions on which conversions can apply - those would have to be "pushed down" as well.

``` c#
var t1 = ("John", 5);   // (string, int)
(object, long) t2 = t1; // What kind of conversion is this? Where is it allowed
```

On the whole we think that, while intuitive, the need for such conversions isn't actually that common. It's hard to construct an example that isn't contrived, involving for instance tuple-typed method parameters and the like. When you really need it, you can deconstruct the tuple and reconstruct it with a tuple literal, making use of target typing.

We'll keep an eye on it, but for now the decision is *not* to propagate element conversions through tuple types. We do recognize that this is a decision we don't get to change our minds on once we've shipped: adding conversions in a later version would be a significant breaking change.

## Projection initializers

Tuple literals are a bit like anonymous types. The latter have "projection initializers" where if you don't specify a member name, one will be extracted from the given expression, if possible. Should we do that for tuples too?

``` c#
var a = new { Name = c.FirstName, c.Age }; // Will have members Name and Age
var t = (Name: c.FirstName, c.Age); // (string Name, int Age) or error?
```

We don't think so. The difference is that names are optional in tuples. It'd be too easy to pick up a random name by mistake, or get errors because two elements happen to pick up the same name.

## Extension methods on tuples

This should just work according to existing rules. That means that extension methods on a tuple type apply even to tuples with different element names:

``` c#
static void M(this (int x, int y) t) { ... }

(int a, int b) t = ...;
t.M(); // Sure
```

## Default parameters

Like other types, you can use `default(T)` to specify a default parameter of tuple type. Should you also be allowed to specify a tuple literal with suitably constant elements?

``` c#
void M((string, int) t = ("Bob", 7)) { ... } // Allowed?
```

No. We'd need to introduce a new attribute for this, and we don't even know if it's a useful scenario.

## Syntax for 0-tuples and 1-tuples?

We lovingly refer to 0-tuples as nuples, and 1-tuples as womples. There is already an underlying `ValueTuple<T>` of size one. We should will also have the non-generic `ValueTuple` be an empty struct rather than a static class.

The question is whether nuples and womples should have syntactic representation as tuple types and literals? `()` would be a natural syntax for nuples (and would no doubt find popularity as a "unit type" alternative to `void`), but womples are harder: parenthesized expressions already have a meaning! 

We made no final decisions on this, but won't pursue it for now.

## Return tuple members directly in scope

There is an idea to let the members of a tuple type appearing in a return position of a method be in scope throughout the method:

``` c#
(string first, string last) GetName()
{
  first = ...; last = ...; // Assign the result directly
  return;                  // No need to return an explicit value
}
```

The idea here is to enhance the symmetry between tuple types and parameter lists: parameter names are in scope, why should "result names"?

This is cute, but we won't do it.  It is too much special casing for a specific placement of tuple types, and it is also actually preferable to be able to see exactly what is returned at a given `return` statement.


# Integrating pattern matching with is-expressions and switch-statements
For pattern matching to feel natural in C# it is vital that it is deeply integrated with existing related features, and does in fact take its queues from how they already work. Specifically we want to extend is-expressions to allow patterns where today they have types, and we want to augment switch-statements so that they can switch on any type, use patterns in case-clauses and add additional conditions to case-clauses using when-clauses.

This integration is not always straightforward, as witnessed by the following issues. In each we need to decide what patterns should *generally* do, and mitigate any breaking changes this would cause in currently valid code.


## Name lookup

The following code is legal today:

``` c#
if (e is X) {...}
switch(e) { case X: ... }
```

We'd like to extend both the places where `X` occurs to be patterns. However, `X` means different things in those two places. In the `is` expression it must refer to a type, whereas in the `case` clause it must refer to a constant. In the `is` expression we look it up as a type, ignoring any intervening members called `X`, whereas in the `case` clause we look it up as an expression (which can include a type), and give an error if the nearest one found is not a constant.

As a pattern we think `X` should be able to both refer to a type and a constant. Thus, we prefer the `case` behavior, and would just stop giving an error when `case X:` refers to a type. For `is` expressions, to avoid a breaking change, we will first look for just a type (today's behavior), and if we don't find one, rather than error we will look again for a constant.

## Conversions in patterns

An `is` expression today will only acknowledge identity, reference and boxing conversions from the run-time value to the type. It looks for "the actual" type, if you will, without representation changes:
 
``` c#
byte b = 5;
WriteLine(b is byte);           // True:  identity conversion
WriteLine((object)b is byte);   // True:  boxing conversion
WriteLine((object)b is object); // True:  reference conversion
WriteLine(b is int);            // False: numeric conversion changes representation
```

This seems like the right semantics for "type testing", and we want those to carry over to pattern matching. 

Switch statements are more weird here today. They have a fixed set of allowed types to switch over (primitive types, their nullable equivalents, strings). If the expression given has a different type, but has a unique implicit conversion to one of the allowed ones, then that conversion is applied! This occurs mainly (only?) when there is a user defined conversion from that type to the allowed one.

That of course is intended only for constant cases. It is not consistent with the behavior we want for type matching per the above, and it is also not clear how to generalize it to switch expressions of arbitrary type. It is behavior that we want to limit as much as possible.

Our solution is that in switches *only* we will apply such a conversion on the incoming value *only* if all the cases are constant. This means that if you add a non-constant case to such a switch (e.g. a type pattern), you will break it. We considered more lenient models involving applying non-constant patterns to the *non*-converted input, but that just leads to weirdness, and we don't think it's necessary. If you *really* want your conversion applied, you can always explicitly apply it to the switch expression yourself.


## Pattern variables and multiple case labels

C# allows multiple case labels on the same body. If patterns in those case labels introduce variables, what does that mean?

``` c#
case int i:
case byte b:
    WriteLine("Integral value!");
    break;
```

Here's what it means: The variables go into the same declaration space, so it is an error to introduce two of the same name in case clauses for the same body. Furthermore, the variables introduced are not definitely assigned, because the given case clause assigns them, and you didn't necessarily come in that way. So the above example is legal, but the body cannot read from the variables `i` and `b` because they are not definitely assigned.

It is tempting to consider allowing case clauses to share variables, so that they could be extracted from similar but different patterns:
``` c#
case (int age, string name):
case (string name, int age):
    WriteLine($"{name} is {age} years old.");
    break;
```

We think that is way overboard right now, but the rules above preserve our ability to allow it in the future.

## Goto case

It is tempting to ponder generalizations of `goto case x`. For instance, maybe you could do the whole switch again, but on the value `x`. That's interesting, but comes with lots of complications and hidden performance traps. Also it is probably not all that useful.

Instead we just need to preserve the simple meaning of `goto case x` from current switches: it's allowed if `x` is constant, if there's a case with the same constant, and that case doesn't have a `when` clause.

## Errors and warnings

Today `3 is string` yields a warning, while `3 as string` yields and error. They philosophy seems to be that the former is just asking a question, whereas the other is requesting a value. Generalized `is` expressions like `3 is string s` are sort of a combination of `is` and `as`, both answering the question and (conditionally) producing a value. Should they yield warnings or errors?

We didn't reach consensus and decided to table this for later.

## Constant pattern equality

In today's `switch` statement, the constants in labels must be implicitly convertible to the governing type (of the switch expression). The equality is then straightforward - it works the same as the `==` operator. This means that the following case will print `Match!`.

``` c#
switch(7)
{
  case (byte)7:
    WriteLine("Match!");
    break;
}
```

What should be the case if we switch on something of type object instead?:

``` c#
switch((object)7)
{
  case (byte)7:
    WriteLine("Match!");
    break;
}
```

One philosophy says that it should work the same way regardless of the static type of the expression. But do we want constant patterns everywhere to do "intelligent matching" of integral types with each other? That certainly leads to more complex runtime behavior, and would probably require calling helper methods. And what of other related types, such as `float` and `double`? There isn't similar intelligent behavior you can do, because the representations of most numbers will differ slightly and a number such as 2.1 would thus not be "equal to itself" across types anyway.

The other option is to make the behavior different depending on the compile-time type of the expression. We'll use integral equality only if we know statically which one to pick, because the left hand side was also known to be integral. That would preserve the switch behavior, but make the pattern's behavior dependent on the static type of the expression.

For now we prefer the latter, as it is simpler.


# Recursive patterns

There are several core design questions around the various kinds of recursive patterns we are envisioning. However, they seem to fall in roughly two categories:

1. Determine the syntactic shape of each recursive pattern in itself, and use that to ensure that the places where patterns can occur are syntactically well-formed and unambiguous.
2. Decide exactly how the patterns work, and what underlying mechanisms enable them.

This is an area to focus more on in the future. For now we're just starting to dig in.

## Recursive pattern syntax

For now we envision three shapes of recursive patterns

1. Property patterns: `Type { Identifier_1 is Pattern_1, ... , Identifier_n is Pattern_n }`
2. Tuple patterns: `(Pattern_1, ... Pattern_n)`
3. Positional patterns: `Type (Pattern_1, ... Pattern_n)`

There's certainly room for evolution here. For instance, it is not lost on us that 2 and 3 are identical except for the presence of a type in the latter. At the same time, the presence of a type in the latter seems syntactically superfluous in the cases where the matched expression is already known to be of that type (so the pattern is used purely for deconstruction and/or recursive matching of the elements). Those two observations come together to suggest a more orthogonal model, where the types are optional:

1. Property patterns: `Type_opt { Identifier_1 is Pattern_1, ... , Identifier_n is Pattern_n }`
2. Positional patterns: `Type_opt (Pattern_1, ... Pattern_n)`

In this model, what was called "tuple patterns" above would actually not just apply to tuples, but to anything whose static type (somehow) specifies a suitable deconstruction.

This is important because it means that "irrefutable" patterns - ones that are known to always match - never need to specify the type. This in turn means that they can be used for unconditional deconstruction even in syntactic contexts where positional patterns would be ambiguous with invocation syntax. For instance, we could have what would amount to a "deconstruction" variant of a declaration statement, that would introduce all its match variables into scope as local variables for subsequent statements:
 
``` c#
(string name, int age) = GetPerson();    // Unconditional deconstruction
WriteLine($"{name} is {age} years old"); // name and age are in scope

```

## How recursive patterns work

Property patterns are pretty straightforward - they translate into access of fields and properties.

Tuple patterns are also straightforward if we decide to handle them specially.

Positional patterns are more complex. We agree that they need a way to be specified, but the scope and mechanism for this is still up for debate. For instance, the `Type` in the positional pattern may not necessarily trigger a type test on the object. Instead it may name a class where a more general "matcher" is defined, which does its own tests on the object. This could be complex stuff, like picking a string apart to see if it matches a certain format, and extracting certain information from it if so.

The syntax for declaring such "matchers" may be methods, or a new kind of user defined operator (like `is`) or something else entirely. We still do not have consensus on either the scope or shape of this, so there's some work ahead of us.

The good news is that we can add pattern matching in several phases. There can be a version of C# that has pattern matching with none or only some of the recursive patterns working, as long as we make sure to "hold a place for them" in the way we design the places where patterns can occur. So C# 7 can have great pattern matching even if it doesn't yet have *all* of it. 
 