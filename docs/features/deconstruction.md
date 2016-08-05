
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
In short, what this does in the general case is find a `Deconstruct` method on the expression on the right-hand-side of the assignment, invoke it, collect its `out` parameters and assign them to the variables on the left-hand-side. And in the special case where the expression on the right-hand-side is a tuple (tuple literal or tuple type), then the elements of the tuple can be assigned to the variables on the left-hand-side without needing to call `Deconstruct`.

The existing assignment binding currently checks if the variable on its left-hand-side can be assigned to and if the two sides are compatible.
It will be updated to support deconstruction-assignment, ie. when the left-hand-side is a tuple-literal/tuple-expression:

- Needs to break the right-hand-side into items. That step is un-necessary if the right-hand-side is already a tuple though.
- Each item on the left needs to be assignable and needs to be compatible with corresponding position on the right (resulting from previous step).
- Needs to handle nesting case such as `(x, (y, z)) = M();`, but note that the second item in the top-level group has no discernable type.

#### Evaluation order

The evaluation order can be summarized as: (1) all the side-effects on the left-hand-side, (2) all the Deconstruct invocations (if not tuple), (3) conversions (if needed), and (4) assignments.

In the general case, the lowering for deconstruction-assignment would translate: `(expressionX, expressionY, expressionZ) = expressionRight` into:

```
// do LHS side-effects
tempX = &evaluate expressionX
tempY = &evaluate expressionY
tempZ = &evaluate expressionZ

// do Deconstruct
evaluate right and evaluate Deconstruct in three parts (tempA, tempB and tempC)

// do conversions
tempConvA = convert tempA
tempConvB = convert tempB
tempConvC = convert tempC

// do assignments
tempX = tempConvA
tempY = tempConvB
tempZ = tempConvC
```

The evaluation order for nesting `(x, (y, z))` is:
```
// do LHS side-effects
tempX = &evaluate expressionX
tempY = &evaluate expressionY
tempZ = &evaluate expressionZ

// do Deconstruct
evaluate right and evaluate Deconstruct into two parts (tempA and tempNested)
evaluate Deconstruct on tempNested intwo two parts (tempB and tempC)

// do conversions
tempConvA = convert tempA
tempConvB = convert tempB
tempConvC = convert tempC

// do assignments
tempX = tempConvA
tempY = tempConvB
tempZ = tempConvC
```

The evaluation order for the simplest cases (locals, fields, array indexers, or anything returning ref) without needing conversion:
```
evaluate side-effect on the left-hand-side variables
evaluate Deconstruct passing the references directly in
```

In the case where the expression on the right is a tuple, the evaluation order becomes:
```
evaluate side-effect on the left-hand-side variables
evaluate the right-hand-side and do a tuple conversion (using a fake tuple representing the types in the left-hand-side)
assign element-wise from the right to the left
```

#### Resolution of the Deconstruct method

The resolution is equivalent to typing `rhs.Deconstruct(out var x1, out var x2, ...);` with the appropriate number of parameters to deconstruct into.
It is based on normal overload resolution.
This implies that `rhs` cannot be dynamic.
Also, the `Deconstruct` method must be an instance method or an extension (but not a static method).

#### Tuple Deconstruction

Note that tuples (`System.ValueTuple`) don't need to invoke Deconstruct.
`System.Tuple` are not recognized as tuples, and so will rely on Deconstruct (which will be provided for up to 3 nestings deep, that is 21 elements)


###Deconstruction-declaration (deconstruction into new variables):

```ANTLR
declaration_statement
    : local_variable_declaration ';'
    | local_constant_declaration ';'
    ;

local_variable_declaration
	: local_variable_type local_variable_declarators
	| deconstruction_declaration // new
	;

deconstruction_declaration // new
	: deconstruction_variables '=' expression
	;

deconstuction_variables
	: '(' deconstuction_variables_nested (',' deconstuction_variables_nested)* ')'
	| 'var' deconstruction_identifiers
	;

deconstuction_variables_nested // new
	: deconstuction_variables
	| type identifier
	;

deconstruction_identifiers
	: '(' deconstruction_identifiers_nested (',' deconstruction_identifiers_nested)* ')'
	;

deconstruction_identifiers_nested // new
	: deconstruction_identifiers
	| identifier
	;

foreach_statement
    : 'foreach' '(' local_variable_type identifier 'in' expression ')' embedded_statement
    | 'foreach' '(' deconstruction_variables 'in' expression ')' embedded_statement // new
    ;

for_statement
    : 'for' '(' for_initializer? ';' for_condition? ';' for_iterator? ')' embedded_statement
    ;

for_initializer
    : local_variable_declaration
    | deconstruction_declaration // new
    | statement_expression_list
    ;

let_clause
    : 'let' identifier '=' expression
    | 'let' deconstruction_identifiers '=' expression // new
    ;

from_clause
    : 'from' type? identifier 'in' expression
	| 'from' deconstuction_variables 'in' expression // new
	| 'from' deconstruction_identifiers 'in' expression // new
    ;
```

It would pick up the behavior of each contexts where new variables can be declared (TODO: need to list). For instance, in LINQ, new variables go into a transparent identifiers.
It is seen as deconstructing into separate variables (we don't introduce transparent identifiers in contexts where they didn't exist previously).

Just like deconstruction-assignment, deconstruction-declaration does not need to invoke `Deconstruct` for tuple types.

**References**

[C# Design Notes for Apr 12-22, 2016](https://github.com/dotnet/roslyn/issues/11031)


