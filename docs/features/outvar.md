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


**ArgumentSyntax node is extended as follows to accommodate for the new syntax:**

- ```Expression``` field is made optional.
- Two new optional fields are added.
```
    <Field Name="Type" Type="TypeSyntax" Optional="true"/>
    <Field Name="Identifier" Type="SyntaxToken" Optional="true">
```


**SemanticModel changes:**

Added new API
```
        /// <summary>
        /// Given an argument syntax, get symbol for an out var that it declares, if any.
        /// </summary>
        /// <param name="declarationSyntax">The syntax node that declares a variable.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The symbol that was declared.</returns>
        public ISymbol GetDeclaredSymbol(ArgumentSyntax declarationSyntax, CancellationToken cancellationToken = default(CancellationToken));
```


**Open issues:**

- Syntax model is still in flux.
- Need to get confirmation from LDM that we really want to make these variables read-only. For now they are just regular, writable, variables. 
- Need to get confirmation from LDM that we really want to disallow referencing out variables the declaring argument list. It seems nothing prevents us from allowing such references for explicitly typed variables. Allowing them for now. 


**TODO:**

[ ] Add tests for scope rules. Given that currently scoping rules match the rules for pattern variables, and implementation takes advantage of existing infrastructure added for pattern variables, the priority of adding these tests is low. We have pretty good suite of tests for pattern variables. 

[ ] Need to get an approval for the new SemanticModel.GetDeclaredSymbol API.
