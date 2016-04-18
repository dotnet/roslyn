Out Variable Declarations
=========================

The *out variable declaration* feature enables a variable to be declared at the location that it is being passed as an `out` argument.

```antlr
argument_value
    : 'out' type identifier
    | ...
    ;
```

A variable declared this way is called an *out variable*. An *out variable* is read-only and scoped to the enclosing statement. More specifically, the scope will be the same as for a *pattern-variable* introduced via pattern-matching.

> **Note**: We may treat *out variables* as *pattern variables* in the semantic model.

You may use the contextual keyword `var` for the variable's type.

> **Open Issue**: The specification for overload resolution needs to be modified to account for the inference of the type of an *out variable*s declared with `var`.

An *out variable* may not be referenced before the close parenthesis of the invocation in which it is defined:

```cs
    M(out x, x = 3); // error
```

> **Note**: There is a discussion thread for this feature at https://github.com/dotnet/roslyn/issues/6183
