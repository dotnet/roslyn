Notation for null-state: `?` means top-level may be `null`, `!` means top-level isn't `null`, and `~` means oblivious.

### Local declaration

Ex: `type local = expr;`

Null-state of `local` is passed through from `expr`.

Produces a `W` warning if top-level nullability of `expr` is `?` but type is `!`.

Ex: `var local = expr;`

Null-state of `local` is passed through from `expr`.

Declared type of `local` has nullability from `expr`.

### Assignment

Ex: `local = expr2`

Ex: `parameter = expr2`

Ex: `refReturningMethod() = expr2`

Ex: `field = expr2`

Null-state of `local` or `parameter` is passed through from `expr2`

Produce a warning if top-level nullability of `expr2` is `?` but type is `!`:

- the warning should be a real warning if the left-hand-side expression is "an API", such as a `ref` or `out` parameter or method, or if it's a field,
- otherwise, it's a `W` warning if it's just a local.

What about ref locals? They could be point to "an API".

### Null-coalescing operator
Ex: `expr1 ?? expr2`

Outputs: top-level null-state of `expr2`

Open issue: should this warn if `expr1` is declared with type non-nullable?

Warn if no best nullability between `expr1` and `expr2` (nested nullability)

### Cast
Ex: `(type)expr`

Produce a `W` warning if top-level nullability of `expr` is `?` but `type` is `!`. 

If `type` is nullable, then the top-level null-state is nullable. (This is useful if you use a method that returns a `string!` but did a lousy job)

Otherwise, the top-level nullability is passed through from `expr` and the type is from the cast `type`.

You can only cast away nested nullability when there is an implicit conversion.

Open issue: what if the user-defined conversion operator returns a nullable?

### Array creation

Ex: `new[] { expr1, expr2 }`

Null-state is a non-null array whose elements' nullability is the most relaxed from the expressions, with order: `?` > `~` > `!`.


### Silencing operator

Ex: `expr!`

Open issue: there are two behaviors: (1) setting the top-level null-state, and (2) suppressing conversion warnings on nested nullability. Does one operator handle both (current POR per Mads), or do we have two syntaxes (`(!)` for suppressing conversion warnings, or maybe `!` and `!!`, ...)

Top-level null-state is non-nullable.

Doesn't suppress warnings.

Doesn't affect nested nullability.

Open issue: confirm null-state when `expr` is oblivious top-level null-state.

Open issue: confirm warning on unnecessary `!`.
