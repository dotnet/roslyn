Notation for null-state: `?` means top-level may be `null`, `!` means top-level isn't `null`, and `~` means oblivious.

----

### Local declaration

Ex: `type local = expr;`

Null-state of `local` is passed through from `expr`.

Produces a `W` warning if top-level nullability of `expr` is `?` but type is `!`.

Ex: `var local = expr;`

Null-state of `local` is passed through from `expr`.

Declared type of `local` has nullability from `expr`.

Open issue: what is the state of `s` declared as `string s = obliviousString;`? (see [issue](https://github.com/dotnet/roslyn/issues/27686))

----
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


----
### Null-coalescing operator
Ex: `expr1 ?? expr2`

Outputs: top-level null-state of `expr2`

Open issue: should this warn if `expr1` is declared with type non-nullable?

Warn if no best nullability between `expr1` and `expr2` (nested nullability)


----
### Cast and conversions

Ex: `(type)expr`

Ex: `var x = (string)maybeNull; // var is a string?`

Ex: `var x = (string)notNull; // var is string`

Ex: `var x = (string?)notNull; // var is a string?`

Warning: Produce a `W` warning if top-level nullability of `expr` is `?` but `type` is `!`. 

If `type` is nullable, then the top-level null-state is nullable. (This is useful if you use a method that returns a `string!` but did a lousy job)

Otherwise, the top-level nullability is passed through from `expr` and the type is from the cast `type`.

Open issue: You can only cast away nested nullability when there is an implicit conversion.

Open issue: suppression of warnings on mismatching nested nullability, should cast handle that scenario or `!`?


----
#### User-defined conversion operator

If a conversion operator takes a `!` as input, but it receives a null-state `?` then produce a real warning.

If a conversion operator returns a `?`, but the cast uses a `!` type then produce a `W` warning.

We need to define the nullability of built-in operators (we should add the correct annotations).


----
### Array creation

Ex: `new[] { expr1, expr2 }`

Null-state is a non-null array whose elements' nullability is the most relaxed from the expressions, with order: `?` > `~` > `!`.


----
### Silencing operator

Ex: `expr!`

Open issue: there are two behaviors: (1) setting the top-level null-state, and (2) suppressing conversion warnings on nested nullability. Does one operator handle both (current POR per Mads), or do we have two syntaxes (`(!)` for suppressing conversion warnings, or maybe `!` and `!!`, ...)

Top-level null-state is non-nullable.

Doesn't affect nested nullability.

Open issue: suppression of warnings on mismatching nested nullability, should cast handle that scenario or `!`?

Open issue: confirm null-state when `expr` is oblivious top-level null-state.

Open issue: confirm warning on unnecessary `!`.


----
### Generic types

(See LDM notes 4/25/2018)

Unconstrained generic types could be null (so `t.ToString()` warns)

You can constrain either with `T : class` (`T` must be non-nullable) or `T : class?` (just a reference type, either nullable or non-nullable). In such cases, since we can't store this information in PE constraints, we'll flag the type parameter itself (and this should round-trip properly through PE).

Similarly, you can do `T : IInterface` (if `T` is a reference type, it must be non-nullable) or `T : IInterface?` (less constrained). You can also do `T : object` (if `T` is a reference type, it must be non-nullable), but `T : object?` is superfluous (constraint does nothing).

We will do some validation on constraints, so as to complain for nullability mismatch on `where T : class, IInterface?`.

`T?` is disallowed for now.

----
### NonNullTypes
The `[NonNullTypes(true)]` attribute is assume by default for source (when the language version is 8.0 or above). It means that reference types like `string` are interpreted to mean non-null string.
The `[NonNullTypes(false)]` attribute causes reference types to be interpreted as oblivious instead.
Note that the `NonNullTypes` only affects the interpretation of types, it does not directly affect the production of warnings.

----
### Warnings
The nullability warnings are produced when using language version 8.0 (or above) and the nullability feature is turned on.
Warnings can be suppressed by usual mechanisms.

Open issue: what compiler flag and UI experience to turn the feature on?

----
### Null tests

We should list the expressions that inform the flow state:
- `expr == null`, `null == expr`, `expr != null`, `null != expr`

No warning for testing something that is already expected to be non-null. This allows for defensively checking input parameters, regardless of annotations.

#### Attribute annotations
- `[EnsuresNotNull]`: `void ThrowsIfNull([EnsuresNotNull] object? o)`
- `[NotNullWhenFalse]`: `bool IsNullOrEmpty([NotNullWhenFalse] string? s)`
- `[AssertsTrue]`: `void Debug.Assert([AssertsTrue] bool condition)`
- `[AssertsFalse]`
- other attributes are being discussed: `[NotNullWhenTrue]` (for `TryGetValue`), null-in null-out, "trust me" fields, equality methods, ref parameters that only set

Open issue: do those annotations apply as arguments are evaluated, or only once the method returns?

----
### Flow analysis

In a general sense, flow analysis visits a bound tree and updates some state. In the case of nullability analysis, the state is tracking nullability information at this point of the code for each variable.
It tracks whether a given variable is maybe-null (nullability `true`), not-null (nullability `false`) or oblivious (nullability `null`).
As the `NullableWalker` visits the bound tree, it not only updates the null-state, it also produces diagnostics and returns the nullability of each visited expression.

As described in the earlier sections of this document, for each kind of bound node the visitor has to decide:
1. how to update the state,
2. whether to produce diagnostics,
3. what result to return.

Let's illustrate each one in turn:
- If you have an assignment `x = "hello"`, the visitor should **update the state** with `x` marked as not-null. Conversely, if you have an assignment `x = null`, the visitor should return with `x` marked as maybe-null (and it may also produce a warning if `x` is declared with a non-nullable type).
- If you have an invocation `x.M()`, the visitor should check the incoming state of `x` and **produce a warning** if it is maybe-null. If the invocation included arguments, such as `x.M(expr1, expr2)`, then the process of visiting the invocation will also visit the arguments, as all three kinds of effects could happen there too (consider `x.M(maybeNull.M2())` or `x.M(x = null)`).
- If you have a field access in an invocation `x.field.M()`, the visitor for `x.field` **returns a result** representing the nullability of that expression, so that the visitor for the invocation may warn if `x.field` is maybe-null.

TODO conditional states `if (condition) { ... } else { ... }` and split state for `condition`

TODO attributes
