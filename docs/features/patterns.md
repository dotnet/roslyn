> Open issues for the design and implementation of this feature can be found at [patterns.work.md](patterns.work.md).

Pattern Matching for C#
=======================

Pattern matching extensions for C# enable many of the benefits of algebraic data types and pattern matching from functional languages, but in a way that smoothly integrates with the feel of the underlying language. The basic features are: [record types](records.md), which are types whose semantic meaning is described by the shape of the data (treated as a separate feature); and pattern matching, which is a new form that enables extremely concise multilevel decomposition of these data types. Elements of this approach are inspired by related features in the programming languages [F#](http://www.msr-waypoint.net/pubs/79947/p29-syme.pdf "Extensible Pattern Matching Via a Lightweight Language") and [Scala](http://lampwww.epfl.ch/~emir/written/MatchingObjectsWithPatterns-TR.pdf "Matching Objects With Patterns").

## Is Expression

The `is` operator is extended to test an expression against a *pattern*.

```antlr
is_pattern_expression
    : relational_expression 'is' pattern
    ;

relational_expression
    : is_pattern_expression
    ;
```

This form of *relational_expression* is in addition to the existing forms in the C# specification. It is a compile-time error if the *relational_expression* to the left of the `is` token does not designate a value or does not have a type.

A *constant_pattern* appearing as the right-hand-side of an *is_pattern_expression* is syntactically restricted to be a *shift_expression*, even though a *constant_pattern* appearing elsewhere can syntactically be any *expression*. For simplicity that restriction is not shown in the grammar.

Every *identifier* of the pattern introduces a new local variable that is *definitely assigned* after the `is` operator is `true` (i.e. *definitely assigned when true*).

> Note: There is technically an ambiguity between *type* in an `is-expression` and *constant_pattern* in an *is_pattern_expression*, either of which might be a valid parse of a qualified identifier. We try to bind it as a type for compatibility with previous versions of the language; only if that fails do we resolve it as a constant pattern.

## Patterns

Patterns are used in the `is` operator and in a *switch_statement* to express the shape of data against which incoming data is to be compared. Patterns may be recursive so that parts of the data may be matched against sub-patterns.

```antlr
pattern
    : declaration_pattern
    | constant_pattern
    | deconstruction_pattern
    | property_pattern
    | discard_pattern
    | var_pattern
    ;

declaration_pattern
    : type simple_designation
    ;

constant_pattern
    : constant_expression
    ;

property_pattern
    : type? property_subpattern simple_designation?
    ;

property_subpattern
    : '{' subpatterns? '}'
    ;

subpatterns
    : subpattern
    | subpattern ',' subpatterns
    ;

subpattern
    : pattern
    | identifier ':' pattern
    ;

deconstruction_pattern
    : type? '(' subpatterns? ')' property_subpattern? simple_designation?
    ;

simple_designation
    : single_variable_designation
    | discard_designation
    ;

discard_pattern
    : discard_designation
    ;

var_pattern
    : 'var' designation
    ;
```

It is a semantic error if any _subpattern_ of a _property_pattern_ does not contain an _identifier_ (it must be of the second form, which has an _identifier_).

There is no special "null-checking pattern", as the ability to check for null falls out as a special case of a trivial property pattern. To check if the string `s` is non-null, you can write any of the following forms

``` c#
if (s is object o) ... // o is of type object
if (s is string x) ... // x is of type string
if (s is {} x) ... // x is of type string
if (s is {}) ...
```

Every pattern is bound against a hypothetical *input operand*. In the case of an *is_pattern_expression*, that is the value of the left-hand-side. In the case of a branch of a `switch` statement, it is the switch expression. In the case of a nested pattern, it is the result of extracting the relevant value from the enclosing object.

### Type Pattern

The *type_pattern* both tests that the input operand holds a value of a given type and casts it to that type if the test succeeds. This introduces a local variable of the given type named by the *simple_designation*. That local variable is *definitely assigned* when the result of the pattern-matching operation is true.

```antlr
type_pattern
    : type simple_designation
    ;
```

The runtime semantic of this expression is that it tests the runtime type of the input operand against the *type* in the pattern. If it is of that runtime type (or some subtype), the result of the `is operator` is `true`. If the *simple_designation* is a *single_variable_designation*, then it declares a new local variable named by the *identifier* that is assigned the value of the input operand when the result is `true`.

Certain combinations of static type of the left-hand-side and the given type are considered incompatible and result in compile-time error. A value of static type `E` is said to be *pattern compatible* with the type `T` if there exists an identity conversion, an implicit reference conversion, a boxing conversion, an explicit reference conversion, or an unboxing conversion from `E` to `T`. It is a compile-time error if an expression of type `E` is not pattern compatible with the type in a type pattern that it is matched with.

> Note: this is not quite correct for type parameters, which should be considered pattern compatible always.

The type pattern is useful for performing run-time type tests of reference types, and replaces the idiom

```cs
Type v;
if (value is Type)
{
    v = (Type)value;
}
```

or

``` c#
var v = expr as Type;
if (v != null) { // code using v }
```

With the slightly more concise

```cs
if (expr is Type v) { // code using v }
```

It is an error if *type* is a nullable value type.

The type pattern can be used to test values of nullable types: a value of type `Nullable<T>` (or a boxed `T`) matches a type pattern `T2 id` if the value is non-null and the type of `T2` is `T`, or some base type or interface of `T`. For example, in the code fragment

```cs
int? x = 3;
if (x is int v) { // code using v }
```

The condition of the `if` statement is `true` at runtime and the variable `v` holds the value `3` of type `int`.

### Constant Pattern

A constant pattern tests the value of an expression against a constant value. The constant may be any constant expression, such as a literal, the name of a declared `const` variable, or an enumeration constant.

If both *e* and *c* are of integral types, the pattern is considered matched if the result of the expression `e == c` is `true`.

Otherwise the pattern is considered matching if `object.Equals(c, e)` returns `true`. In this case it is a compile-time error if the static type of *e* is not *pattern compatible* with the type of the constant.

```antlr
constant_pattern
    : constant_expression
    ;
```

### Var Pattern

``` antlr
var_pattern
    : 'var' designation
    ;
```

An input operand matches the pattern `var identifier` always. In other words, a match to a *var pattern* with a *simple_designator* always succeeds. At runtime the value of the input operand is bound to a newly introduced local variable. The type of the local variable is the static type of the input operand.

It is an error if the name `var` binds to a type where a *var_pattern* is used.

> Note: we need to describe the semantics when the *designation* is a *tuple_designation*.

### Discard Pattern

An input operand matches the discard `_` always. In other words, every expression matches the discard pattern.

### Property Pattern

``` antlr
property_pattern
	: type? property_subpattern simple_designation?
	;

property_subpattern
	: '{' subpatterns? '}'
	;

subpatterns
	: subpattern
	| subpattern ',' subpatterns
	;

subpattern
	: pattern
	| identifier ':' pattern
	;
```

A property pattern tests the input operand to see if it is an instance of a given type, and also tests some of its accessible properties or fields to see if they match given subpatterns.

The type to be tested is selected as follows:
- If there is a *type* part of the *property_pattern*, then that is the type to be tested. The type must not be a nullable value type.
- Otherwise if the input operand is not a nullable value type, its static type is used.
- Otherwise the underlying type of the input operand's type is used.

The *simple_designation*, if it is a *single_variable_designation*, names a newly introduced variable of this type.

Each of the *subpattern*s appearing int the *property_subpattern* specifies a property or field to be checked. The subpattern must be of the second form, with the *identifier* present. The *identifier* must name an accessible and readable instance property or field of the type to be tested.

At runtime, a *subpattern* is *satisfied* if the value of that property or field, when treated as the input operand of the *subpattern*'s pattern, matches.

A property pattern matches if the type test succeeds and all subpatterns are satisfied. The order in which subpatterns are matched is not specified, and a failed match may not test all subpatterns at runtime.

### Deconstruction Pattern

A deconstruction pattern is similar to a *property_pattern*, but involves the deconstruction of a *tuple* or the invocation of a user-defined *Deconstruct* method.

``` antlr
deconstruction_pattern
	: type? '(' subpatterns? ')' property_subpattern? simple_designation?
	;
```

It is an error if a deconstruction pattern has a single subpattern but omits the type.

The type to be tested is determined as in the *property_pattern*. That type must be a tuple type whose *cardinality* is the same as the number of *subpatterns* between parentheses, or it must be a type that contains a unique `Deconstruct` method with that number of `out` parameters, as defined for the *deconstruction assignment* feature.

A subpattern between parentheses in a *deconstruction_pattern* is satisfied if the value retrieved from the input operand's deconstruction for that position, when treated as the input operand for the corresponding pattern, matches. If such a subpattern has an *identifier*, it is a compile-time error if that is not the name of the corresponding tuple element or `Deconstruct` `out` parameter.

A deconstruction pattern matches if the type test succeeds and all subpatterns are satisfied. The order in which subpatterns are matched is not specified, and a failed match may not test all subpatterns at runtime.

> Note: this specification does not yet treat `ITuple` for positional matching.
> 

## Switch Expression

A *switch_expression* is added to support `switch`-like semantics for an expression context.

The C# language syntax is augmented with the following syntactic productions:


``` antlr
switch_expression
    : null_coalescing_expression switch '{' switch_expression_case_list '}'
    ;
switch_expression_case_list
    : switch_expression_case
    | switch_expression_case_list ',' switch_expression_case
    ;
switch_expression_case
    : pattern where_clause? '=>' expression
    ;
```

The *switch_expression* is not allowed as an *expression_statement*.

The type of the *switch_expression* is the *best common type* of the expressions appearing to the right of the `=>` tokens of the *switch_expression_case*s.

It is an error if the compiler can prove (using a set of techniques that has not yet been specified) that some *switch_section*'s pattern cannot affect the result because some previous pattern will always match.

At runtime, the result of the *switch_expression* is the value of the *expression* of the first *switch_expression_case* for which the expression on the left-hand-side of the *switch_expression* matches the *switch_expression_case*'s pattern, and for which the expression of the *where_clause* of the *switch_expression_case*, if present, evaluates to `true`.

> Note: we need to specify what happens if the set of cases is incomplete, both at compile-time and at runtime.

## Some Examples of Pattern Matching

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
    case Const(_): return Const(0);
    case Add(var Left, var Right):
      return Add(Deriv(Left), Deriv(Right));
    case Mult(var Left, var Right):
      return Add(Mult(Deriv(Left), Right), Mult(Left, Deriv(Right)));
    case Neg(var Value):
      return Neg(Deriv(Value));
  }
}
```

An expression simplifier demonstrates positional patterns:

```cs
Expr Simplify(Expr e)
{
  switch (e) {
    case Mult(Const(0), _): return Const(0);
    case Mult(_, Const(0)): return Const(0);
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
