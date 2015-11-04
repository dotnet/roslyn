Pattern Matching for C#
=======================

Pattern matching extensions for C# enable many of the benefits of algebraic data types and pattern matching from functional languages, but in a way that smoothly integrates with the feel of the underlying language. The basic features are: [record types](records.md), which are types whose semantic meaning is described by the shape of the data; and pattern matching, which is a new expression form that enables extremely concise multilevel decomposition of these data types. Elements of this approach are inspired by related features in the programming languages [F#](http://www.msr-waypoint.net/pubs/79947/p29-syme.pdf "Extensible Pattern Matching Via a Lightweight Language") and [Scala](http://lampwww.epfl.ch/~emir/written/MatchingObjectsWithPatterns-TR.pdf "Matching Objects With Patterns").

## Is Expression

The `is` operator is extended to test an expression against a *pattern*.

>*relational-expression*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*relational-expression* `is` *complex-pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*relational-expression* `is` *type*

It is a compile-time error if *e* does not designate a value or does not have a type.

Every *identifier* of the pattern introduces a new local variable that is *definitely assigned* after the `is` operator is `true` (i.e. *definitely assigned when true*).

## Patterns

Patterns are used in the `is` operator and in a *switch-statement* to express the shape of data against which incoming data is to be compared. Patterns may be recursive so that subparts of the data may be matched against subpatterns.

>*complex-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*type* *identifier*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*recursive-pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*recursive-pattern* *identifier*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*property-pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*property-pattern* *identifier*

>*recursive-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*type* `(` *subpattern-list*<sub>opt</sub> `)`

>*property-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*type* `{` *property-pattern-list* `}`

>*property-pattern-list*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*identifier* `is` *pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*identifier* `is` *pattern* `,` *property-pattern-list*

>*subpattern-list*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*subpattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*subpattern-list* `,` *subpattern*

>*subpattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*argument-name*<sub>opt</sub> *pattern*

>*pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*simple-pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*complex-pattern*

>*simple-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*constant-pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*wildcard-pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`var` *identifier*

>*wildcard-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`*`

>*constant-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*shift-expression*

### Type Pattern

The type pattern both tests that an expression is of a given type and casts it to that type if the test succeeds. This introduces a local variable of the given type named by the given identifier. That local variable is *definitely assigned* when the is operator is true.

>*complex-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*type* *identifier*

The runtime semantic of this expression is that it tests the runtime type of the left-hand *relational-expression* operand against the *type* in the pattern. If it is of that runtime type (or some subtype), the result of the `is operator` is `true` and the local variable is assigned the value of the left-hand operand.

Certain combinations of static type of the left-hand-side and the given type are considered incompatible and result in compile-time error. A value of static type `E` is said to be *pattern compatible* with the type `T` if there exists an identity conversion, an implicit reference conversion, a boxing conversion, an explicit reference conversion, or an unboxing conversion from `E` to `T`. It is a compile-time error if an expression of type `E` is not pattern compatible with the type in a type pattern that it is matched with.

The type pattern is useful for performing runtime type tests of reference types, and replaces the idiom

```
var v = expr as Type;
if (v != null) { // code using v }
```

With the slightly more concise

```
if (expr is Type v) { // code using v }
```

It is an error if *type* is a nullable value type.

The type pattern can be used to test values of nullable types: a value of type `Nullable<T>` (or a boxed `T`) matches a type pattern `T2 id` if the value is non-null and the type is `T2` is `T`, or some base type or interface of `T`. For example, in the code fragment

```
int? x = 3;
if (x is int v) { // code using v }
```

The condition of the `if` statement is `true` at runtime and the variable `v` holds the value `3` of type `int` inside the block.

### Constant Pattern

A constant pattern tests the runtime value of an expression against a constant value. The constant may be any constant expression, such as a literal, the name of a declared `const` variable, or an enumeration constant.

An expression *e* matches a constant pattern *c* if `object.Equals(e, c)` returns `true`.

>*constant-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*constant-expression*

It is a compile-time error if the static type of *e* is not *pattern compatible* with the type of the constant.

| Note |
| --- |
| We plan to relax the rules for matching constants so that a literal such as `1` would match a value of any integral type (`byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, or `ulong`) whose value is `1`. |

### Var Pattern

An expression *e* matches the pattern `var identifier` always. In other words, a match to a *var pattern* always succeeds. At runtime the value of *e* is bounds to a newly introduced local variable. The type of the local variable is the static type of *e*.

### Wildcard Pattern

An expression *e* matches the pattern `*` always. In other words, every expression matches the wildcard pattern.

### Recursive Pattern

A recursive pattern enables the program to invoke an appropriate `operator is`, and (if the operator returns `true`) perform further pattern matching on the values that are returned from it. In the absence of an `operator is`, if the named type was defined with a *parameter list*, then the properties declared in the type's parameters are read to match subpatterns.

>*recursive-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*type* `(` *subpattern-list*<sub>opt</sub> `)`

Given a match of an expression *e* to the pattern *type* `(` *subpattern-list*<sub>opt</sub> `)`, a method is selected by searching in *type* for accessible declarations of `operator is` and selecting one among them using *match operator overload resolution*. It is a compile-time error if the expression *e* is not *pattern compatible* with the type of the first argument of the selected operator.

- If a suitable `operator is` exists, at runtime, the value of the expression is tested against the type of the first argument as in a type pattern. If this fails then the recursive pattern match fails and the result is `false`. If it succeeds, the operator is invoked with fresh compiler-generated variables to receive the `out` parameters. Each value that was received is matched against the corresponding *subpattern*, and the match succeeds if all of these succeed. The order in which subpatterns are matched is not specified, and a failed match may not match all subpatterns.
- If no suitable `operator is` was found, and *type* designates a type that was defined with a parameter list, the number of subpatterns must be the same as the number of parameters of the type. In that case the properties declared in the type's parameter list are read and matched against the subpatterns, as above.
- Otherwise it is an error.

If a *subpattern* has an *argument-name*, then every subsequent *subpattern* must have an *argument-name*. In this case each argument name must match a parameter name (of an overloaded `operator is` in the first bullet above, or of the type's parameter list in the second bullet). [Note: this needs to be made more precise.]

#### Property Pattern

A property pattern enables the program to recursively match values extracted by the use of properties.

>*property-pattern*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*type* `{` *property-pattern-list* `}`
>*property-pattern-list*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*identifier* `is` *pattern*
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*identifier* `is` *pattern* `,` *property-pattern-list*

Given a match of an expression *e* to the pattern *type* `{` *property-pattern-list* `}`, it is a compile-time error if the expression *e* is not *pattern compatible* with the type *T* designated by *type*.

At runtime, the expression is tested against *T*. If this fails then the property pattern match fails and the result is `false`. If it succeeds, then each of the identifiers appearing on the left-hand-side of its *property-pattern-list* must designate a readable property or field of *T*. Each such field or property is matched against its corresponding pattern, and the result of the whole match is `false` only if the result of any of these is `false`. The order in which subpatterns are matched is not specified, and a failed match may not match all subpatterns at runtime.

### Scope of Pattern Variables

The scope of a pattern variable is as follows:

- If the pattern appears in the condition of an `if` statement, its scope is the condition and controlled statement of the `if` statement, but not its `else` clause.
- If the pattern appears in the `when` clause of a `catch`, its scope is the *catch-clause*.
- If the pattern appears in a *switch-label*, its scope is the *switch-section*.
- If the pattern appears in a *match-label*, its scope is the *match-section*.
- If the pattern appears in the `when` clause of a *switch-label* or *match-label*, its scope of that *switch-section* or *match-section*.
- If the pattern appears in the body of an expression-bodied lambda, its scope is that lambda's body.
- If the pattern appears in the body of an expression-bodied method or property, its scope is that expression body.
- If the pattern appears in the body of an expression-bodied local function, its scope is that method body.
- If the pattern appears in a *ctor-initializer*, its scope is the constructor body.
- If the pattern appears in a field initializer, its scope is that field initializer.
- Otherwise if the pattern appears directly in some *statement*, its scope is that *statement*.

Other cases are errors for other reasons (e.g. in a parameter's default value or an attribute, both of which are an error because those contexts require a constant expression).

The use of a pattern variables is a value, not a variable. In other words pattern variables are read-only.

## User-defined operator is

An explicit `operator is` may be declared to extend the pattern matching capabilities. Such a method is invoked by the `is` operator or a *switch-statement* with a *recursive-pattern*.

For example, suppose we have a type representing a Cartesian point in 2-space:

```
public class Cartesian
{
	public int X { get; }
	public int Y { get; }
}
```

We may sometimes think of them in polar coordinates:

```
public static class Polar
{
	public static bool operator is(Cartesian c, out double R, out double Theta)
	{
		R = Math.Sqrt(c.X*c.X + c.Y*c.Y);
		Theta = Math.Atan2(c.Y, c.X);
		return c.X != 0 || c.Y != 0;
	}
}
```

And now we can operate on `Cartesian` values using polar coordinates

```
var c = Cartesian(3, 4);
if (c is Polar(var R, *)) Console.WriteLine(R);
```

Which prints `5`.

## Switch Statement

The `switch` statement is extended to select for execution the first block having an associated pattern that matches the *switch expression*.

>*switch-label*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`case`  *complex-pattern* *case-guard*<sub>opt</sub> `:`
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`default :`

> *case-guard*:
> &nbsp;&nbsp;&nbsp;&nbsp;`when` *expression*

[TODO: we need to explain the interaction with definite assignment here.]

The order in which patterns are matched is not defined. A compiler is permitted to match patterns out of order, and to reuse the results of already matched patterns to compute the result of matching of other patterns.

In some cases the compiler can prove that a switch section can have no effect at runtime because its pattern is subsumed by a previous case. In these cases a warning may be produced. [TODO: these warnings should be mandatory and we should specify precisely when they are produced.]

If a *case-guard* is present, its expression of type `bool`. It is evaluated as an additional condition that must be satisfied for the case to be considered satisfied.

## Match Expression

A *match-expression* is added to support `switch`-like semantics for an expression context.

The C# language syntax is augmented with the following syntactic productions:

> *relational-expression*:
> &nbsp;&nbsp;&nbsp;&nbsp;*match-expression*

We add the *match-expression* as a new kind of *relational-expression*.

> *match-expression*:
> &nbsp;&nbsp;&nbsp;&nbsp;*relational-expression* `switch` *match-block*

> *match-block*:
> &nbsp;&nbsp;&nbsp;&nbsp;`(` *match-sections* `)`

At least one *match-section* is required.

> *match-sections*:
> &nbsp;&nbsp;&nbsp;&nbsp;*match-section*
> &nbsp;&nbsp;&nbsp;&nbsp;*match-sections* *match-section*
>
> *match-section*:
> &nbsp;&nbsp;&nbsp;&nbsp;`case` *pattern* *case-guard*<sub>opt</sub> `:` *expression*
>
> *case-guard*:
> &nbsp;&nbsp;&nbsp;&nbsp;`when` *expression*

It is not proposed that *match-expression* be added to the set of syntax forms allowed as an *expression-statement*.

The type of the *match-expression* is the *least common type* of the expressions appearing to the right of the `:` tokens of the *match section*s.

It is an error if the compiler can prove (using a set of techniques that has not yet been specified) that some *match-section*'s pattern cannot affect the result because some previous pattern will always match.

At runtime, the result of the *match-expression* is the value of the *expression* of the first *match-section* for which the expression on the left-hand-side of the *match-expression* matches the *match-section*'s pattern, and for which the *case-guard* of the *match-section*, if present, evaluates to `true`.

## Throw expression

We extend the set of expression forms to include

>*throw-expression*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;`throw` *null-coalescing-expression*
*null-coalescing-expression*:
&emsp;&emsp;&emsp;&emsp;&emsp;&emsp;*throw-expression*

The type rules are as follows:

- A *throw-expression* has no type.
- A *throw-expression* is convertible to every type by an implicit conversion.

The flow-analysis rules are as follows:

- For every variable *v*, *v* is definitely assigned before the *null-coalescing-expression* of a *throw-expression* iff it is definitely assigned before the *throw-expression*.
- For every variable *v*, *v* is definitely assigned after *throw-expression*.

A *throw expression* is allowed in only the following contexts:
- As the second or third operand of a ternary conditional operator `?:`
- As the second operand of a null coalescing operator `??`
- After the colon of a *match section*
- As the body of an expression-bodied lambda or method.

## Destructuring assignment

Inspired by an [F# feature](https://msdn.microsoft.com/en-us/library/dd233238.aspx) and a [conversation on github](https://github.com/dotnet/roslyn/issues/5154#issuecomment-151974994), we could support decomposition like this:

> *block-statement:*
&nbsp;&nbsp;&nbsp;&nbsp;*let-statement*
*let-statement:*
&nbsp;&nbsp;&nbsp;&nbsp;`let` *identifier* `=` *expression* `;`
&nbsp;&nbsp;&nbsp;&nbsp;`let` *complex-pattern* `=` *expression* `;`
&nbsp;&nbsp;&nbsp;&nbsp;`let` *complex-pattern* `=` *expression* `;` `else` *embedded-statement*

`let` is an existing contextual keyword.

The form
> `let` *identifier* `=` *expression* `;`

is shorthand for 

> `let` `var` *identifier* `=` *expression* `;`

(i.e. a *var-pattern*) and is a convenient way for declaring a read-only local variable.

Semantically, it is an error unless precisely one of the following is true
1. the compiler can prove that the expression always matches the pattern; or
2. an `else` clause is present.

If an `else` clause is present, it is an error if the endpoint of its *embedded-statement* is reachable.

Any pattern variables in the *pattern* are in scope throughout the enclosing block and are definitely assigned (only) after the execution of the *let-statement* completes. It is an error to use these variables before their point of definition.

A *let-statement* is a *block-statement* and not an *embedded-statement* because its primary purpose is to introduce names into the enclosing scope. It therefore does not introduce a dangling-else ambiguity.

## Some Possible Optimizations

The compilation of pattern matching can take advantage of common parts of patterns. For example, if the top-level type test of two successive patterns in a *switch-statement* is the same type, the generated code can skip the type test for the second pattern.

When some of the patterns are integers or strings, the compiler can generate the same kind of code it generates for a switch-statement in earlier versions of the language.

For more on these kinds of optimizations, see [[Scott and Ramsey (2000)]](http://www.cs.tufts.edu/~nr/cs257/archive/norman-ramsey/match.pdf "When Do Match-Compilation Heuristics Matter?").

It would be possible to support declaring a type hierarchy closed, meaning that all subtypes of the given type are declared in the same assembly. In that case the compiler can generate an internal tag field to distinguish among the different subtypes and reduce the number of type tests required at runtime. Closed hierarchies enable the compiler to detect when a set of matches are complete. It is also possible to provide a slightly weaker form of this optimization while allowing the hierarchy to be open.

## Some Examples of Pattern Matching

### Is-As

We can replace the idiom

```cs
var v = expr as Type;	
if (v != null) {
    // code using v
}
```

With the slightly more concise and direct

```cs
if (expr is Type v) {
    // code using v
}
```

### Testing nullable

We can replace the idiom

```cs
Type? v = x?.y?.z;
if (v.HasValue) {
    var value = v.GetValueOrDefault();
    // code using value
}
```

With the slightly more concise and direct

```cs
if (x?.y?.z is Type value) {
    // code using value
}
```

### Arithmetic simplification

Suppose we define a set of recursive types to represent expressions (per a separate proposal):

```cs
abstract class Expr;
class X() : Expr;
class Const(double Value) : Expr;
class Add(Expr Left, Expr Right) : Expr;
class Mult(Expr Left, Expr Right) : Expr;
class Neg(Expr Value) : Expr;
```

Now we can define a function to compute the (unreduced) derivative of an expression:

```cs
Expr Deriv(Expr e)
{
  switch (e) {
    case X(): return Const(1);
    case Const(*): return Const(0);
    case Add(var Left, var Right):
      return Add(Deriv(Left), Deriv(Right));
    case Mult(var Left, var Right):
      return Add(Mult(Deriv(Left), Right), Mult(Left, Deriv(Right)));
    case Neg(var Value):
      return Neg(Deriv(Value));
  }
}
```

An expression simplifier demonstrates recursive patterns:

```cs
Expr Simplify(Expr e)
{
  switch (e) {
    case Mult(Const(0), *): return Const(0);
    case Mult(*, Const(0)): return Const(0);
    case Mult(Const(1), var x): return Simplify(x);
    case Mult(var x, Const(1)): return Simplify(x);
    case Mult(Const(var l), Const(var r)): return Const(l*r);
    case Add(Const(0), var x): return Simplify(x);
    case Add(var x, Const(0)): return Simplify(x);
    case Add(Const(var l), Const(var r)): return Const(l+r);
    case Neg(Const(var k)): return Const(-k);
    default: return e;
  }
}
```

### A match expression (contributed by @orthoxerox):

```cs
var areas =
    from primitive in primitives
    let area = primitive switch (
        case Line l: 0
        case Rectangle r: r.Width * r.Height
        case Circle c: Math.PI * c.Radius * c.Radius
        case *: throw new ApplicationException()
    )
    select new { Primitive = primitive, Area = area };
```

### Tuple decomposition

The *let-statement* would apply to tuples as follows. Given

```cs
public (int, int) Coordinates => …
```

You could receive the results into a block scope thusly

```cs
	let (int x, int y) = Coordinates;
```

(This assumes much about the tuple spec and the interaction of tuples and pattern-matching, all of which is unsettled.)

### Roslyn diagnostic analyzers

Much of the Roslyn compiler code base, and client code written to use Roslyn for producing user-defined diagnostics, could have its core logic simplified by using syntax-based pattern matching.

### Cloud computing applications

[NOTE: This section needs much more explanation and examples.]

* Records are very convenient for communicating data in a distributed system (client-server and server-server).
  It is also useful for returning multiple results from an async method.
* "Views", or user-written operator "is", is useful for treating, for example,
 json as if it is an application-specific data structure. Pattern matching is very convenient for
 dispatching in an actors framework.

## A Open Issues

*	Pattern matching with anonymous types.
*	Pattern matching with arrays.
*	Pattern matching with List<T>, Dictionary<K, V>
*	How should the type `dynamic` interact with pattern matching?
*	In what situations should the compiler be required by specification to warn about a match that must fail. E.g. switch on a `byte` but case label is out of range?
*	Should it be possible to combine a recursive pattern and a property pattern into a single pattern, like `Point(int x, int y) { Length is 5 }` ?
*	Should it be possible to name the whole matched thing in a property and/or recursive pattern, like `case Point(2, int x) p:` ?
