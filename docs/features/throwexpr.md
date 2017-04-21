## Throw expression

We extend the set of expression forms to include

```antlr
throw_expression
    : 'throw' null_coalescing_expression
    ;

null_coalescing_expression
    : throw_expression
    ;
```

The type rules are as follows:

- A *throw_expression* has no type.
- A *throw_expression* is convertible to every type by an implicit conversion.

The flow-analysis rules are as follows:

- For every variable *v*, *v* is definitely assigned before the *null_coalescing_expression* of a *throw_expression* iff it is definitely assigned before the *throw_expression*.
- For every variable *v*, *v* is definitely assigned after *throw_expression*.

A *throw expression* is permitted in only the following syntactic contexts:
- As the second or third operand of a ternary conditional operator `?:`
- As the second operand of a null coalescing operator `??`
- As the body of an expression-bodied lambda or method.

> Note: the rest of the semantics of the throw expression are identical to the semantics of the *throw_statement* in the current language specification.
