Out Variable Declarations
=========================

The *out variable declaration* feature enables a variable to be declared at the location that it is being passed as an `out` argument.

```antlr
argument_value
    : 'out' type identifier
    | ...
    ;
```

A variable declared this way is called an *out variable*. 
You may use the contextual keyword `var` for the variable's type.
The scope will be the same as for a *pattern-variable* introduced via pattern-matching.

According to Language Specification (section 7.6.7 Element access)
The argument-list of an element-access is not allowed to contain ref or out arguments.
However, due to backward compatibility, compiler overlooks this restriction during parsing
and even ignores out/ref modifiers in element access during binding.
We will enforce that language rule for out variables declarations at the syntax level.

Within the scope of a local variable introduced by a local-variable-declaration, 
it is a compile-time error to refer to that local variable in a textual position 
that precedes its declaration. 

It is also an error to reference implicitly-typed (§8.5.1) out variable in the same argument list that immediately 
contains its declaration.

Overload resolution is modified as follows:

We add a new conversion:

> There is a *conversion from expression* from an implicitly-typed out variable declaration to every type.

The section "Better Conversion from expression" (from the ECMA version, which is the base of our draft C# spec) is modified to add the bold text, below:

> 13.6.4.4 Better conversion from expression
Given an implicit conversion C1 that converts from an expression E to a type T1, and an implicit conversion C2 that converts from an expression E to a type T2, C1 is a better conversion than C2 if at least one of the following holds:
- E has a type S and an identity conversion exists from S to T1 but not from S to T2
- E is not an anonymous function **or implicitly-typed out variable declaration,** and T1 is a better conversion target than T2 (§13.6.4.6)
- E is an anonymous function, T1 is either a delegate type D1 or an expression tree type `Expression<D1>`, T2 is either a delegate type D2 or an expression tree type `Expression<D2>` and one of the following holds:
  - D1 is a better conversion target than D2
  - D1 and D2 have identical parameter lists, and one of the following holds:
    - D1 has a return type Y1, and D2 has a return type Y2, an inferred return type X exists for E in the context of that parameter list (§13.6.3.13), and the conversion from X to Y1 is better than the conversion from X to Y2
    - E is async, D1 has a return type `Task<Y1>`, and D2 has a return type `Task<Y2>`, an inferred return type `Task<X>` exists for E in the context of that parameter list (§13.6.3.13), and the conversion from X to Y1 is better than the conversion from X to Y2
    - D1 has a return type Y, and D2 is `void` returning

The type of an implicitly-typed out variable is the type of the corresponding parameter in the signature of the method.

The new syntax node `DeclarationExpressionSyntax` is added to represent the declaration in an out var argument.

#### Discussion

There is a discussion thread for this feature at https://github.com/dotnet/roslyn/issues/6183.

#### Open issues and TODOs:

Tracked at https://github.com/dotnet/roslyn/issues/11566.
