Discards
--------

Discards are variables which you can assign to, but cannot read from. They don't have names. Instead, they are represented by an '_' (underscore).
In C#7.0, they can appear in the following contexts:

- out variable declarations, such as `bool found = TryGetValue(out var _)` or `bool found = TryGetValue(out _)`
- deconstruction assignments, such as `(x, _) = deconstructable;`
- deconstruction declarations, such as `(var x, var _) = deconstructable;`
- is patterns, such as `x is int _`
- switch/case patterns, such as `case int _:`

The principal representation of discards is an `_` (underscore) designation in a declaration expression. For example, `int _` in an out variable declaration or `var (_, _, x)` in a deconstruction declaration.

The second representation of discards is using the expression `_` as a short-hand for `var _`, when no variable named `_` is in scope. It is allowed in out vars, deconstruction assignments and declarations, and plain assignments (`_ = IgnoredReturn();`). It is, however, not allowed in C#7.0 patterns.
When a variable named `_` does exist in scope, then the expression `_` is simply a reference to that variable, as it did in earlier versions of C#.

###Grammar changes

```ANTLR
declaration_expression
	: type variable_designation
	;

variable_designation
	: single_variable_designation
	| parenthesized_variable_designation
	| discard_designation // new
	;

discard_designation // new
	: '_'
	;
```

**References**
[C# Language Design Notes for Oct 25 and 26, 2016](https://github.com/dotnet/roslyn/issues/16640)
[C# Design Notes for Oct 18, 2016](https://github.com/dotnet/roslyn/issues/16482)
[Design proposal for discards](https://github.com/dotnet/roslyn/issues/14862)
Note that those notes used to refer to discards as "wildcards".
