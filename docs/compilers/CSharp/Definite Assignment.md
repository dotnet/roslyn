Definite Assignment
===================

The definite assignment rules for C# were reimplemented in Roslyn, and we discovered some shortcomings of the specification and native compiler, which we document here.

### Constant boolean expressions

There are two foundations definite assignment rules missing from the C# language specification, and from the implementation of definite assignment in the native compiler. Absence of these two rules causes a large number of cases of nonintuitive behavior of the compiler, and so the native compiler also includes a large number of ad-hoc rules to patch these examples.

The Roslyn compiler implements the C# language specification with two additional foundational rules. With these rules added, the ad-hoc rules implemented by the native compiler are no longer needed. The new rules are more precise than those implemented by the native compiler; for example, the Roslyn C# compiler accepts the following code, but the native compiler rejects it:

```cs
    static void Main(string[] args)
    {
        int x;
        if (false && x == 3)
        {
            x = x + 1; // Dev10 does not consider x definitely assigned
        }
    }
```

The new foundational rules are:

> #### 5.3.3.N Constant Expressions
For a constant expression with value true:
 - If *v* is definitely assigned before the expression,
   then *v* is definitely assigned after the expression.
 - Otherwise *v* is “definitely assigned after false expression”
   after the expression.

>For a constant expression with value false:
 - If *v* is definitely assigned before the expression,
   then *v* is definitely assigned after the expression.
 - Otherwise *v* is “definitely assigned after true expression” after the expression.

>For all other constant expressions, the definite assignment state of *v* after the expression is the same as the definite assignment state of *v* before the expression.

### Control transfers and intervening finally blocks

The specification for definite assignment and reachability in the C# language specification do not properly take into account the possibility of "intervening" finally blocks between the origin of the control transfer and the destination. These finally blocks can assign to variables, this changing the definite assignment status, or change the control transfer behavior (e.g. by throwing an exception). Both the native compiler and the Roslyn C# compilers account for them in computing definite assignment and reachability of statements.

### Definite assignment of structs across assemblies

The compiler previously had a bug where it did not consider private fields of reference type inside structs as participating in
piecewise definite assignment of variables of that struct type. For example,

```C#
public struct S
{
    private object _f;
}

---

public class C
{
   void M()
   {
      S s; // s is now considered definitely assigned
   }
}
```

In the previous example, `s` will be considered not definitely assigned if `S` and `C` are in the same assembly, but will
be considered definitely assigned if they are in different assemblies. This behavior is preserved in newer compilers for
backwards compatibility, but it does not conform to any version of the C# language specification.
