Overload Resolution
===================

Besides the small overload resolution changes to the C# language spec in C# 6, the Roslyn C# compiler implements some overload resolution rules not in the specification in order to behave in a way compatible with the (pre C# 6) native compiler. These changes are supported by unit tests in `OverloadResolutionTests.cs` and `OperatorTests.cs`.

Here are some of the changes

### No conversion from old-syntax anon delegate to expression tree

Like the native compiler, Roslyn C# will not convert an old-style anonymous method expression to an expression tree.

```cs
using System;
using System.Linq.Expressions;
class Program
{
    static void Main()
    {
        Foo(delegate { }); // No error; chooses the non-expression version.
    }
    static void Foo(Action a) { }
    static void Foo(Expression<Action> a) { }
}
```

### Enum operator -

The native compiler had some odd behavior around the subtraction operator on enum types. These are unlikely to affect real code, but we reproduce them to ensure we don't break programs by changing their behavior.

The old compiler implemented the built-in operator

```cs
	E operator-(I, E)
```

where `E` is an enum type with underlying type `I`.

A precise reading of the spec suggests that, for an enum variable `e` where the enum's underlying type is not int, the expression `e - 0` is ambiguous (the 0 can be converted to either the enum type or the underlying type, but neither conversion is an exact match). We resolve the ambiguity the same as the old compiler did.

You can turn off these compatibility features using the compiler flag
/features:strict

### Tie-breaking rule with multiple interface inheritance

The old compiler implemented special rules for overload resolution (not in the language specification) in the presence of optional and `params ` parameters, and Roslyn's more strict interpretation of the specification (now fixed) prevented some programs from compiling.

Specifically, a problem occurred if two extension methods with the same name are implemented on different interfaces, and both interfaces are extended by one single interface, called e.g. `IFinalInterface`, so that this interface contains two extension methods with the same name. 

The first extension method has a `params int[]` as parameter: 

```cs
public static int Properties(this IFirstInterface source, params int[] x) 
{ 
    return 0; 
}
```
 
The second extension method has an additional optional parameter: 

```cs
public static bool Properties(this ISecondInterface source, int x = 0, params int[] y) 
{ 
return true; 
} 
```

Now, the following call would cause an ambiguous error:

```cs 
var x = default(IFinalInterface); 
var properties = x.Properties();
```

See #2298 for more details, and #2305 for the implementation and tests that enable Roslyn to compile this code.

### Tie-breaking rule with unused param-array parameters

The old compiler implemented special rules for overload resolution (not in the language specification) in the presence of unused param-array parameters, and Roslyn's more strict interpretation of the specification (now fixed) prevented some programs from compiling.

Specifically, a problem occurred if two methods have the same parameter types with exception of param-array types, and an attempt is made to call them without supplying any arguments for param-array parameter.

```
public class Test
{
    public Test(int a, params string[] p) { }
    public Test(int a, params List<string>[] p) { }
}
```

Now, the following call would cause an ambiguous error:

```
var x = new Test(10);
```

Old compiler calls the second overload (```Test(int a, params List<string>[] p)```)

See #4458 for more details, and #4761 for the implementation and tests that enable Roslyn to compile this code.