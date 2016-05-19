
Deconstruction
--------------

This design doc will cover two kinds of deconstruction: deconstruction into existing variables (deconstruction-assignment) and deconstruction into new variables (deconstruction-declaration).
It is still very much work-in-progress.

Here is an example of deconstruction-assignment:
```C#
class C
{
    static void Main()
    {
        long x;
        string y;

        (x, y) = new C();
        System.Console.WriteLine(x + "" "" + y);
    }

    public void Deconstruct(out int a, out string b)
    {
        a = 1;
        b = ""hello"";
    }
}
```

Treat deconstruction of a tuple into existing variables as a kind of assignment, using the existing AssignmentExpression.


###Deconstruction-assignment (deconstruction into existing variables):
This doesn't introduce any changes to the language grammar. We have an `assignment-expression` (also simply called `assignment` in the C# grammar) where the `unary-expression` (the left-hand-side) is a `tuple-literal`.
In short, what this does is find a `Deconstruct` method on the expression on the right-hand-side of the assignment, invoke it, collect its `out` parameters and assign them to the variables on the left-hand-side.

The existing assignment binding currently checks if the variable on its left-hand-side can be assigned to and if the two sides are compatible.
It will be updated to support deconstruction-assignment, ie. when the left-hand-side is a tuple-literal/tuple-expression:

- Each item on the left needs to be assignable and needs to be compatible with corresponding position on the right
- Needs to handle nesting case such as `(x, (y, z)) = M();`, but note that the second item in the top-level group has no discernable type.

The lowering for deconstruction-assignment would translate: `(expressionX, expressionY, expressionZ) = (expressionA, expressionB, expressionC)` into:

```
tempX = &evaluate expressionX
tempY = &evaluate expressionY
tempZ = &evaluate expressionZ

tempRight = evaluate right and evaluate Deconstruct

tempX = tempRight.A (including conversions)
tempY = tempRight.B (including conversions)
tempZ = tempRight.C (including conversions)

“return/continue” with newTupleIncludingNames tempRight (so you can do get Item1 from the assignment)?
```

The evaluation order for nesting `(x, (y, z))` is:
```
tempX = &evaluate expressionX

tempRight = evaluate right and evaluate Deconstruct

tempX = tempRight.A (including conversions)
tempLNested = tempRight.B (no conversions)

tempY = &evaluate expressionY
tempZ = &evaluate expressionZ

tempRNest = evaluate Deconstruct on tempRight

tempY = tempRNest.B (including conversions)
tempZ = tempRNest.C (including conversions)

```

The evaluation order for the simplest cases (locals, fields, array indexers, or anything returning ref) without needing conversion:
```
evaluate side-effect on the left-hand-side variables
evaluate Deconstruct passing the references directly in
```

Note that the feature is built around the `Deconstruct` mechanism for deconstructing types.
`ValueTuple` and `System.Tuple` will rely on that same mechanism, except that the compiler may need to synthesize the proper `Deconstruct` methods.


**Work items, open issues and assumptions to confirm with LDM:**

- I assume this should work even if `System.ValueTuple` is not present.
- How is the Deconstruct method resolved?
    - I assumed there can be no ambiguity. Only one `Deconstruct` is allowed (in nesting cases we have no type to guide the resolution process).
    - But we may allow a little bit of ambiguity and preferring an instance over extension method.
- Do the names matter? `int x, y; (a: x, b: y) = M();`
- Can we deconstruct into a single out variable? I assume no.
- I assume no compound assignment `(x, y) += M();`
- [ ] Provide more details on the semantic of deconstruction-assignment, both static (The LHS of the an assignment-expression used be a L-value, but now it can be L-value -- which uses existing rules -- or tuple_literal. The new rules for tuple_literal on the LHS...) and dynamic.
- [ ] Discuss with Aleksey about "Deconstruct and flow analysis for nullable ref type"
- [ ] Validate any target typing or type inference scenarios.
- The deconstruction-assignment is treated separately from deconstruction-declaration, which means it doesn't allow combinations such as `int x; (x, int y) = M();`.

###Deconstruction-declaration (deconstruction into new variables):

```ANTLR
declaration_statement
    : local_variable_declaration ';'
    | local_constant_declaration ';'
    | local_variable_combo_declaration ';'  // new
    ;

local_variable_combo_declaration
    : local_variable_combo_declaration_lhs '=' expression

local_variable_combo_declaration_lhs
    : 'var' '(' identifier_list ')'
    | '(' local_variable_list ')'
    ;

identifier_list
    : identifier ',' identifier
    | identifier_list ',' identifier
    ;

local_variable_list
    : local_variable_type identifier ',' local_variable_type identifier
    | local_variable_list ',' local_variable_type identifier
    ;

foreach_statement
    : 'foreach' '(' local_variable_type identifier 'in' expression ')' embedded_statement
    | 'foreach' '(' local_variable_combo_declaration_lhs 'in' expression ')' embedded_statement // new
    ;

for_initializer
    : local_variable_declaration
    | local_variable_combo_declaration // new
    | statement_expression_list
    ;

let_clause
    : 'let' identifier '=' expression
    | 'let' '(' identifier_list ')' '=' expression // new
    ;

from_clause // not sure
    : 'from' type? identifier 'in' expression
    ;

join_clause // not sure
    : 'join' type? identifier 'in' expression 'on' expression 'equals' expression
    ;

join_into_clause // not sure
    : 'join' type? identifier 'in' expression 'on' expression 'equals' expression 'into' identifier
    ;

constant_declarator // not sure
    : identifier '=' constant_expression
    ;
```

Treat deconstruction of a tuple into new variables as a new kind of node (AssignmentExpression).
It would pick up the behavior of each contexts where new variables can be declared (TODO: need to list). For instance, in LINQ, new variables go into a transparent identifiers.
It is seen as deconstructing into separate variables (we don't introduce transparent identifiers in contexts where they didn't exist previously).

Should we allow this?
`var t = (x: 1, y: 2);    (x: var a, y: var b) = t;`
or `var (x: a, y: b) = t;`
(if not, tuple names aren't very useful?)

- [ ] Add example: var (x, y) =
- [ ] Semantic (cardinality should match, ordering including conversion,
- [ ] What are the type rules? `(string s, int x) = (null, 3);`

- Deconstruction for `System.ValueTuple`, `System.Tuple` and any other type involves a call to `Deconstruct`.

**References**

[C# Design Notes for Apr 12-22, 2016](https://github.com/dotnet/roslyn/issues/11031)


