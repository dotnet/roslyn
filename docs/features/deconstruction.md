
Quickstart guide for deconstructions (C# 7.0)
----------------------------------------------
1. Install Visual Studio 2017 
2. Start a C# project
3. Add a reference to the `System.ValueTuple` package from NuGet  
![Install the ValueTuple package](img/install-valuetuple.png)
4. Use deconstructions:

```C#
public class C
{
        public static void Main()
        {
                
              int code;
              string message;

              var pair = (42, "hello");
              (code, message) = pair; // deconstruct a tuple into existing variables
              Console.Write(message); // hello

              (code, message) = new Deconstructable(); // deconstruct any object with a proper Deconstruct method into existing variables
              Console.Write(message); // world
              
              (int code2, string message2) = pair; // deconstruct into new variables
              var (code3, message3) = new Deconstructable(); // deconstruct into new 'var' variables
        }
}

public class Deconstructable
{
        public void Deconstruct(out int x, out string y)
        {
                x = 43;
                y = "world";
        }
}
```

Design
------
This design doc will cover two kinds of deconstruction: deconstruction into existing variables (*deconstruction-assignment*) and deconstruction into new variables (*deconstruction-declaration*).

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

### Deconstruction-assignment (deconstruction into existing variables):

This doesn't introduce any changes to the language grammar. We have an *assignment-expression* (also simply called *assignment* in the C# grammar) where the *unary-expression* (the left-hand-side) is a *tuple-expression* containing values that can be assigned to.
In short, what this does in the general case is find a `Deconstruct` method on the expression on the right-hand-side of the assignment, invoke it with the appropriate number of `out var` parameters, converts those output values (if needed) and assign them to the variables on the left-hand-side. But in the special case where the expression on the right-hand-side is a tuple (tuple expression or tuple type), then the elements of the tuple are assigned to the variables on the left-hand-side without calling Deconstruct.

If the left-hand-side is nested the process will be repeated. For instance, in `(x, (y, z)) = deconstructable;`, `deconstructable` will be deconstructed into two parts and its second part will be further deconstructed. 

In the case where the expression on the right is a tuple expression, it is first given a type. So in `long x; string y; (x, y) = (1, null);` the literals on the right-hand-side are typed as `long` and `string` before the deconstruction even starts, which means that no conversions will be needed during the deconstruction steps.

We noted already that tuples (which are syntactic sugar for the `System.ValueTuple` underlying type) don't need to invoke `Deconstruct`.
The .NET framework also includes a set of `System.Tuple` types. Those are not recognized as C# tuples, and so will rely on the *Deconstruct* pattern. Those `Deconstruct` methods will be provided as extension methods for `System.Tuple` for up to 3 nestings deep (that is 21 elements).

A *deconstruction-assignment* returns a tuple value (with elements using default names) which is shaped and typed like the left-hand-side and holds the (converted) parts resulting from deconstruction.

#### Evaluation order

The evaluation order can be summarized as: (1) all the side-effects on the left-hand-side, (2) all the `Deconstruct` invocations (if not tuple), (3) conversions (if needed), and (4) assignments.

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

#### Resolution of the Deconstruct method

The resolution is equivalent to typing `rhs.Deconstruct(out var x1, out var x2, ...);` with the appropriate number of parameters to deconstruct into.
It is based on normal overload resolution.
This implies that `rhs` cannot be dynamic and that none of the parameters of the `Deconstruct` method can be type arguments. A `Deconstruct<T>(out T x1, out T x2)` method will not be found.
Also, the `Deconstruct` method must be an instance method or an extension (but not a static method). It also must return `void`.


### Deconstruction-declaration (deconstruction into new variables):

The *deconstruction-declaration* is also represented with an *assignment*, but the left-hand-side is either a *declaration-expression* or a tuple expression containing *declaration-expressions*.
*Deconstruction-declarations* can be thought of as two steps: (1) declaring new locals, and (2) applying a *deconstruction-assignment* into those locals.
The declaration of new locals by the left-hand-side of the assignment can take multiple forms. The simplest case is `(int x, string y)`, that is a tuple expression containing declaration expressions. Variants include nested declarations like `(int x, (string y, long z))` (which declares 3 locals) and implicitly typed declarations like `(var x, var y)`. The latter can also be written using the shorthand `var (x, y)`, which is a declaration expression with parenthesized designations.
`var` is the only case where such shorthand is allowed (so `int (x, y)` is not legal).

As in the case of *deconstruction-declarations*, tuple expressions on the right-hand-side have their type inferred from the left-hand-side. With *deconstruction-declaration*, this is also the case, except that any type is `var` in the left-hand-side, then the natural type of the element in the right-hand-side is used.
For example, in `(string x, byte y, var z) = (null, 1, 2);`, `null` has type `string`, the literal `1` has type `byte` (inferred from `y`) and the literal `2` has type `int` (its natural type).

In C#7.0, *deconstruction-declarations* are only allowed as top-level statements. They do not allow mixing of declaration expressions and assignable expressions (such as `(existing, var declared) = (1, 2)`).

### Grammar changes

```ANTLR
expression
	: ... // existing
	| declaration_expression // new (only allowed in C#7.0 in certain contexts, such as out var, deconstruction and pattern declarations)
	;

declaration_expression // new
	: type variable_designation
	;

variable_designation // new
	: single_variable_designation
	| parenthesized_variable_designation
	| discard_designation
	;

single_variable_designation // new
	: identifier
	;

parenthesized_variable_designation // new
	: '(' variable_designation (',' variable_designation)+ ')'
	;

discard_designation // new
	: '_'
	;

foreach_variable_statement // new
    : 'foreach' '(' declaration_expression 'in' expression ')' embedded_statement
    ;
```

**References**

[C# Design Notes for Oct 25-26, 2016](https://github.com/dotnet/csharplang/blob/main/meetings/2016/LDM-2016-10-25-26.md)

[C# Design Notes for Sep 6, 2016](https://github.com/dotnet/csharplang/blob/main/meetings/2016/LDM-2016-09-06.md)

[C# Design Notes for July 13, 2016](https://github.com/dotnet/csharplang/blob/main/meetings/2016/LDM-2016-07-13.md)

[C# Design Notes for May 3-4, 2016](https://github.com/dotnet/csharplang/blob/main/meetings/2016/LDM-2016-05-03-04.md)

[C# Design Notes for Apr 12-22, 2016](https://github.com/dotnet/csharplang/blob/main/meetings/2016/LDM-2016-04-12-22.md)

[Design for declaration expressions](https://github.com/dotnet/csharplang/issues/365)

The [What's new in C# 7.0](https://blogs.msdn.microsoft.com/dotnet/2016/08/24/whats-new-in-csharp-7-0) post has a section on deconstructions.

**Possible future expansions**
- deconstruction in [let and from clause](https://github.com/dotnet/csharplang/issues/189) 
- deconstruction in [lambda argument lists](https://github.com/dotnet/csharplang/issues/258)
- [deconstruction patterns](https://github.com/dotnet/csharplang/issues/277)
- allowing deconstructions with [mix of assignment and declaration](https://github.com/dotnet/csharplang/issues/125), thus also allowing deconstruction-declarations in expression contexts

See [C# Lang](https://github.com/dotnet/csharplang) repo for more up-to-date proposals and discussions.
