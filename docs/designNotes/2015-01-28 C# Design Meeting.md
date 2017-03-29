C# Design Meeting Notes for Jan 28, 2015
========================================

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/180.

Quote of the day: 

> It's not turtles all the way down, it's frogs. :-)

Agenda
------


1. Immutable types
2. Safe fixed-size buffers
3. Pattern matching
4. Records

See also [Language features currently under consideration by the language design group](https://github.com/dotnet/roslyn/issues?q=is%3Aopen+label%3A%22Area-Language+Design%22+label%3A%221+-+Planning%22+ "Language Features Under Consideration").

1. Immutable types
==================

In research prototypes we've experimented with an `immutable` modifier on types, indicating that objects of the type are *deeply* immutable - they recursively do not point to mutable fields. Issue #159 describes the proposal in more detail.

How do we construct types which once fully constructed can't be changed? 

- all fields are readonly, and  recursively have immutable types
- can only inherit from other immutable types (or `object`)
- the constructor can't use "this" other than to access fields

`unsafe` or some other notation could be used to escape scrutiny, in order to create "observable immutability" while cheating under the hood (typically for performance reasons). You could factor such unsafeness into a few types, e.g. `Lazy<T>`.

The feature is designed to work with generics. There would be a new constraint `where T: immutable`. However don't want to bifurcate on `Tuple<T>` vs `ImmutableTuple<T>` just based on whether the content type is constrained to `immutable`. 

Instead an immutable `Tuple<T>` would instantiate to immutable types *only* if type arguments are all immutable. So `Tuple<int>` would be immutable, `Tuple<StringBuilder>` wouldn't be. Immutable generic types would allow type parameters in fields, because that still maintains the recursive guarantee.

Immutable interfaces are also part of the proposal. Somewhat strangely an immutable interface can only be implemented by types that pass all their non-immutable type parameters to the interface!

What's the value? It's mostly a tool for the compiler to help you ensure you are following your intent of writing deeply immutable types. 

Why is that a valuable property to ensure in objects?
- it would allow safe parallel operations over them (modulo holes, see below)
- it would allow burning an object graph into the DLL by compile time evaluation of static initializers
- they can be passed to others without defensive copying

Immutable delegates are ones that can only bind to methods on immutable types. At the language level, that means closures would need to be generated as immutable when possible - which it won't often be, unless we adopt readonly parameters and locals (#98, #115).

As for choice of keyword: `readonly` indicates "shallow", that's why `immutable` may be a better word.

Given the restrictions, you'd expect that any method call on an immutable type would have side effects only on data that was passed in to the method - so a parameterless method (or one taking only immutable parameters) would essentially be pure.

Unfortunately that is not quite true. This expectation can be undermined by two things (other than the built-in facility for cheating): mutable static fields and reflection. We can probably live with reflection, that's already a way to undermine so many other language level expectations! However, mutable statics are unfortunate, not just for this scenario but in general. It would be a breaking change to start disallowing them, of course, bu they could be prevented with a Roslyn analyzer.

Even then, while not having side effects, calling such a method twice with the same arguments might not yield the same result: even returning a new object isn't idempotent.

Given the holes and gotchas, the question is whether it is still valuable enough to have this feature? If it's not a full guarantee but mostly a help to not make mistakes, maybe we should do this through attributes and analyzers? The problem with analyzers is that you can't rely on other folks to run them on their code that you depend on. It wouldn't e.g. prevent defensive copies.

In our research project, this turned out to be very valuable in detecting bugs and missed optimization opportunities.

The object freezing could be done without the feature just by carefully analyzing static fields. But the feature might better help people structure things to be ripe for it.

IDE tooling benefits: Extract method would not need to grab all structs by ref.

We would have to consider making it impossible to implement an immutable interface even via the old compiler. Otherwise there's a hole in the system. Something with "modrec"?

If we added a bunch of features that introduce readonly objects to C# 7 (like records, tuples, ...) and then add this feature later, would we end up being in trouble? Only if we violated the rules we would end up applying.

Marking an existing unsealed type as immutable would be a breaking change. If we introduce such classes in C# 7, it would be breaking to make them immutable later.

As a case study, has Roslyn suffered from the lack of this feature? There have been race conditions, but those would still have happened. Is Roslyn not a great example?

Probably not. Roslyn is *not* immutable. It's presenting an immutable *view*, but is mutable inside. Would that be the common case, though? 

Some of the "cheating" in Roslyn (caching, free lists, etc) is for performance, some is for representing cycles. Insofar as the immutable types feature is *also* for performance, it seems that thhere's a tension between using it or not.

In summary, we are unsure of the value. Let's talk more.


2. Safe Fixed-Size buffers
==========================

Why are fixed-size buffers unsafe? We could generate safe code for them - at least when not in unsafe regions.  Proposal described at #126.

It might generate a lot of code. You could do it with different syntax, or switch on how you generate.

This is a very constrained scenario. It wouldn't be harmful, and a few people would celebrate, but is it worth our effort?

It would allow arbitrary types, not just primitive like today. That may be the bigger value.

Not a high-pri, not one of the first we should commit to.


3. Pattern matching
===================

A view on pattern matching:

A pattern is not an expression, but a separate construct. It can be recursive.
It's idempotent, doesn't affect the state of the program or "compute" something.

Sktech of possible pattern syntax:

> *pattern:*
&emsp; `*`
&emsp; *literal*
&emsp; `var` *identifier*
&emsp; *type* *identifier*<sub>opt</sub>
&emsp; *type* `{` *member* `is` *pattern* ... `}` *identifier*<sub>opt</sub>

> *expression:*
&emsp; *expression* `is` *pattern* 

> *switch-label:*
&emsp; `case` *pattern* `:` 


Actually we'd separate into simple and complex patterns and only allow some at the top level.

We'd have to think carefully about semantics to make it fit into existing is expressions and switch statements. Alternatively we'd come up with a new kind of switch statement. The syntax of switch is already among the least appealing parts of C# - maybe time for a revamp anyway?

Additionally we could imagine a switch *expression*, e.g. of the form:

``` c#
match (e) { pattern => expression; ... ; default => expression }
```
This would result in a value, so it would have to be complete - exactly one branch would have to be taken. Maybe an expression would be allowed to throw. In fact, `throw` could be made an expression.

Expanded `is` operator
----------------------

Here's an example scenario from the Roslyn framework. This should not be taken to say that this is a feature specific to building compilers, though! First, without pattern matching:

``` c#
var e = s as ExpressionStatement;
if (e != null) {
    var a = e.Expr as AssignmentExpressionSyntax;
    if (a != null) {
        var l = a.Left as IdentifierName;
        var r = a.RIght as IdentifierName;
        if (l != null && r != null & l.Name.name == r.Name.name) ...
```

Ugh! With just the `e is T x` non-recursive pattern match we can do a lot better, handling everything in a single if condition:

``` c#
if (s is ExpressionStatement e &&
    e.Expr is AssignmentExpressionSyntax a &&
    a.Left is IdentifierName l &&
    a.Right is IdentifierName r &&
    l.Name.name == r.Name.name) ...
```

Much more readable! The explicit null checks and the nesting are gone, and everything still has sensible names.

The test digs pretty deep inside the structure. Here's what if would look like with recursive `T { ... }` patterns:

``` c#
if (s is ExpressionStatement {
        Expr is AssignmentExpressionSyntax {
            Left is IdentifierName { Name is val l },
            Right is IdentifierName { Name is val r } } }
    && l.name = r.name) ...
```

Here the pattern match sort of matches the structure of the object itself, nesting patterns for nested objects. It is not immediately obvious which is more readable, though - at least we disagree enthusiastically on the design team about that.

It is possible that the nesting approach has more things going for it, though:

- patterns might not just be type tests - we could embrace "active patterns" that are user defined
- we could do optimized code gen here, since evaluation order might not necessarily be left to right.
- the code becomes more readable because the structure of the code matches the shape of the object.
- the approach of coming up with more top-level variable names may run out of steam the deeper you go.

The `T { ... }` patterns are a little clunky, but would apply to any object. For more conciseness, we imagine that types could specify positional matching, which would be more compactly matched through a `T (...)` pattern.That would significantly shrink the recursive example:

``` c#
if (s is ExpressionStatement(
        AssignmentExpressionSyntax(IdentifierName l, IdentifierName r)) 
    && l.name = r.name) ...
```

This is more concise, but it relies on you knowing what the positions stand for since you are not giving them a name at the consumption site.

So, in summary, it's interesting that the `e is T x` form in itself gives most of the value. The incremental improvement of having recursive patterns may not be all that significant.

Either way, the built-in null check is likely to help with writing null-safe code.

On the tooling side, what would the stepping behavior be? One answer is that there is no stepping behavior. That's already the case inside most expressions. Or we could think it through and come up with something better if the need is there.

The variables are definitely assigned only when true. There is a scoping issue with else if. It's similar to the discussions around declaration expressions in C# 6: 

``` c#
if (o is string s) { ... s ... }
else if (o is short s) { ... s ... } // error: redeclaration of s
else ...
```

All else equal, though not definitely assigned there, the `string s` introduced in the first if condition would also be in scope in the else branch, and the nested if clause would therefore not be allowed to introduce another variable with the same name `s`.

With declaration expressions we discussed having special rules around this, e.g. making variables introduced in an if condition *not* be in scope in the else clause. But this gets weird if you try to negate the condition and swap the branches: all of a sudden the variables would be in scope only where they are *not* definitely assigned.

Expanded `switch` statement
---------------------------

The above examples are in the context of if(...is...), but for switch statements you can't just put `&&...` after the initial pattern. Maybe we would allow a `where ...` (or `when`) in the case labels to add additional filtering:

``` c#
switch (o) {
case ExpressionStatement(
        AssignmentExpressionSyntax(IdentifierName l, IdentifierName r)
        where (l.name == r.name):
    ...
}
```

If we add pattern matching to switch statements, one side effect is that we generalize the type of thing you can switch on to anything classified as a value.

``` c#
object o = ...
switch(o) {
    case 1:
    case 2:
    case 3:
    case Color.Red:
    case string s:
    case *: // no
    case var x: // would have to be last and there'd have to not be a default:
    default:
}
```
We would probably disallow `*`, the wildcard, at the top level. It's only useful in recursive positional notation.

Evaluation order would now be important. For back compat, we would allow default everywhere, and evaluate it last.

We would diagnose situations where an earlier pattern hides a later one:

``` c#
case Point(*, 2):
case Point(1, 2): // error/warning
```

We could allow case guards, which would make the case not subsume other cases:

``` c#
case Point(var x, 2) where (x > 2): 
case Point(1, 2): // fine
```

User-defined `is` operator
--------------------------

We've sneaked uses of a positional pattern match above. For that to work, a type would have to somehow specify which positional order to match it in. One very general approach to this would be to allow declaration of an `is` operator on types:

``` c#
public class Point
{
    public Point(int x, int y) { this.X = x; this.Y = y; }
    public int X { get; }
    public int Y { get; }
    overrides public int GetHashCode() ...
    overrides public bool Equals(...)...
    public static bool operator is(Point self out int x, out int y) {...}
}
```

The operator `is` is a particular way of specifying custom matching logic, similar to F# active patterns. We could imagine less ambitious ways, in particular if we just want to specify deconstruction and not additional logic. That's something to dive into later.

4. Records
==========

A value-semantics class like the above would be automatically generated by a "record" feature, e.g. from something like:

``` c#
class Point(int X, int Y);
```

By default, this would generate all of the above, except parameter names would be upper case. If you want to supercede default behavior, you can give it a body and do that explicitly. For instance, you could make X mutable:

``` c#
class Point(int X, int Y)
{
    public int X { get; set; } = X;
}
```

You could have syntax to create separate parameter names from member names:

``` c#
class Point(int x:X, int y:Y)
{
    public int X { get; set; } = x;
}
```

Whether in this form or otherwise, we definitely want to pursue more concise type declarations that facilitate value semantics and deconstruction. We want to pursue the connection with anonymous types and we want to pursue tuples that mesh well with the story too.

