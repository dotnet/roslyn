C# Design Meeting 2015-03-25
============================

Discussion thread for these notes can be found at https://github.com/dotnet/roslyn/issues/1572.

These are notes that were part of a presentation on 2015-03-25 of a snapshot of our design discussions for C# 7 and VB 15. The features under discussion are described in detail in #206 and elsewhere.

[Records](https://github.com/dotnet/roslyn/issues/206)
=======

Changes since Semih Okur's work:
- Remove `record` modifier
- `with` expressions (#5172)
- Working on serialization

Working proposal resembles Scala case classes with active patterns.

------

```cs
class Point(int X, int Y);
```

- defines a class (struct) and common members
	- constructor
	- `readonly` properties
	- GetHashCode, Equals, ToString
	- operator== and operator!= (?)
	- Pattern-matching decomposition operator
- an association between ctor parameters and properties
- Any of these can be replaced by hand-written code
- Ctor-parameter and property can have distinct names

------

### Use Cases
- Simplifies common scenarios
- Simple immutable user-defined data types
	- Roslyn Syntax Trees, bound nodes
- Over-the-wire data
- Multiple return values

------

### With expressions

Illustrates an example of the value of having parameter-property association.

Given

```cs
struct Point(int X, int Y);
Point p = ...
```

the expression

```cs
p with { Y = 4 }
```

is translated to

```cs
new Point(p.X, 4)
```

------

### Open issues
- Closed hierarchy (and tag fields)
- Readonly
- Parameter names
- Can you opt out of part of the machinery?
- Serialization (in all of its forms)
- Construction and decomposition syntax

------

[Pattern Matching](https://github.com/dotnet/roslyn/issues/206)
================

### Sources of Inspiration
- Scala
- F#
- Swift
- Rust
- Erlang
- Nemerle

------

### A pattern-matching operation
- Matches a *value* with a *pattern*
- Either *succeeds* or *fails*
- Extracts selected values into *variables*

```cs
    object x = ...;
    if (x is 3) ...
	if (x is string s) ...
	if (x is Point { X is 3, Y is int y }) ...
    if (x is Point(3, int y)) ...
```

------

### Other aspects
- Patterns defined recursively
- "select" from among a set of pattern forms
- Typically an expression form
- *active patterns* support interop and user-defined types

```cs
switch (o)
{
    case 3:
		...
		break;
	case string s:
		M(s);
		break;
	case Point(3, int y):
		M(y);
		break;
	case Point(int x, 4):
		M(x);
		break;
}
```

We think we want an expression form too (no proposed syntax yet, but for inspiration):

```cs
   M(match(e) {
		3 => x,
		string s => s.foo,
		Point(3, int y) => y,
		* => null })
```

------

### Benefits
- Condenses long sequences of complex logic with a test that resembles the shape of the thing tested

### Use Cases
- Simplifies common scenarios
- Language manipulation code
	- Roslyn
	- Analyzers
- Protocol across a wire

------

### Open questions
- How much of the pattern-matching experience do we want (if any)?
- Matching may be compiler-checked for completeness
- May be implemented using tags instead of type tests
- Which types have syntactic support?
	- Primitives and string
	- Records
	- Nullable<T>
	- objects with properties
	- anonymous types?
	- arrays?
	- List? Dictionary?
	- Tuple<...>?
	- IEnumerable<T>?
