Notes on Records and Pattern Matching for 2015-10-07 design review
==================================================================

Discussion for these notes can be found at https://github.com/dotnet/roslyn/issues/5757.

### Records and Pattern Matching (https://github.com/dotnet/roslyn/issues/206)

Prototyped together last year by @semihokur, but that prototype is
based on very out-of-date Roslyn code. We also have some design changes
since that time and we want to separate a pattern-matching prototype from
records/ADTs so we can make independent decisions about whether
and when to include them in the language(s).

First step is to port pattern matching to the latest sources.
In-progress port at https://github.com/dotnet/roslyn/pull/4882

### Spec changes since the 2014 prototype

For pattern matching:

1. Scoping of pattern-introduced variables (with "funny" rule for `if`)
2. Rules for `switch` statement that make it a compatible extension of the existing construct (https://github.com/dotnet/roslyn/issues/4944)
3. An expression form of multi-arm pattern-matching (https://github.com/dotnet/roslyn/issues/5154)
4. A `when` clause added to `switch` cases.

And, for records:

1. No `record` keyword necessary
2. `with` expressions (https://github.com/dotnet/roslyn/issues/5172)
3. Approach for for algebraic data types

### Implementation status of prototype port

1. For pattern matching, checklist at https://github.com/dotnet/roslyn/pull/4882 tracking the progress
2. For records, port not started

### Making the extension of `switch` backward-compatible

- We say that the cases are matched in order, except `default` which is always the last
resort.

- Integral-typed case labels match any integral-valued control expression with the same value.

- One issue around user-defined conversions to switchable types is
resolved (https://github.com/dotnet/roslyn/issues/4944). In the draft spec,
a conversion will be applied on the `case`s, not on the control-expression unilaterally.
Instead of converting only to `swithable` types, each
`case` arm will consider any conversions that allow the `case` to be applied.
Any given conversion would be applied at most once. 

```cs
Foo foo = ...; // has a conversion to int
switch (foo)
{
    case 1: // uses the converted value
    case Foo(2): // uses the original value
    case 3: // uses the converted value
}
```

- The `goto case` statement is extended to allow any expression as its argument.

### Expression form of multi-arm pattern matching (https://github.com/dotnet/roslyn/issues/5154)

```cs
var areas =
    from primitive in primitives
    let area = primitive match (
        case Line l: 0
        case Rectangle r: r.Width * r.Height
        case Circle c: Math.PI * c.Radius * c.Radius
        case *: throw new ApplicationException()
    )
    select new { Primitive = primitive, Area = area };
```

There is no `default` here, so cases are handled strictly in order.

I propose the spec require that the compiler "prove" that all cases are handled
in a `match` expression using not-yet-specified rules. Writing those rules
is an open work item, but I imagine it will require the compiler to build
a decision tree and check it for completeness. That will also be needed to
implement checks that no case is subsumed by a previous case, which will
cause a warning (for `switch`) or error (for `match`).

### With-expressions (https://github.com/dotnet/roslyn/issues/5172)

```cs
class Point(int X, int Y, int Z);
...
    Point p = ...;
    Point q = p with { Y = 2 };
```

The latter is translated into

```cs
    Point q = new Point(X: p.X, Y: 2, Z: p.Z);
```

We know how to do this for record types (because the language specifies the
mapping between constructor parameters and properties). We're examining how
to extend it to more general types.

To support inheritance, rather than directly using the constructor (as above) the generated code will
invoke a compiler-generated (but user-overridable) factory method.

```cs
    Point q = p.With(X: p.X, Y: 2, Z: p.Z);
```

### Draft approach for algebraic data types

```cs
abstract sealed class Expression
{
    class Binary(Operator Operator, Expression Left, Expression Right) : Expression;
    class Constant(int Value) : Expression;
}
```

None of these classes would be permitted to be extended elsewhere.
a `match` expression that handles both `Binary` and `Constant` cases
would not need a `*` (default) case, as the compiler can prove it
is complete.

### Remaining major issues

1. We need to specify the rules for checking
 - If the set of cases in a `match` is complete 
 - If a `case` is subsumed by a previous `case`

2. We need more experience with algebraic data types and active patterns.

3. Can we extend `with` expressions to non-record types?
