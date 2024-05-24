# Deviations from the C# standard

This document lists inconsistencies between Roslyn and the C# standard where they are known, organized by standard section.

# Conversions

## Implicit enum conversions from zero

From [§10.2.4](http://csharpstandard/standard/conversions.md#1024-implicit-enumeration-conversions):

> An implicit enumeration conversion permits a *constant_expression* ([§11.21](http://csharpstandard/standard/expressions.md#1121-constant-expressions)) with any integer type and the value zero to be converted to any *enum_type* and to any *nullable_value_type* whose underlying type is an *enum_type*.

Roslyn performs implicit enumeration conversions from constant expressions with types of `float`, `double` and `decimal` as well:

```csharp
enum SampleEnum
{
    Zero = 0,
    One = 1
}

class EnumConversionTest
{
    const float ConstFloat = 0f;
    const double ConstDouble = 0d;
    const decimal ConstDecimal = 0m;

    static void PermittedConversions()
    {
        SampleEnum floatToEnum = ConstFloat;
        SampleEnum doubleToEnum = ConstDouble;
        SampleEnum decimalToEnum = ConstDecimal;
    }
}
```

Conversions are (correctly) *not* permitted from constant expressions which have a type of `bool`, other enumerations, or reference types.

# Member lookup

From [§12.5.1](https://github.com/dotnet/csharpstandard/blob/draft-v8/standard/expressions.md#125-member-lookup):

> - Finally, having removed hidden members, the result of the lookup is determined:
>   - If the set consists of a single member that is not a method, then this member is the result of the lookup.
>   - Otherwise, if the set contains only methods, then this group of methods is the result of the lookup.
>   - Otherwise, the lookup is ambiguous, and a binding-time error occurs.

Roslyn instead implements a preference for methods over non-method symbols:

```csharp
var x = I.M; // binds to I1.M (method)
x();

System.Action y = I.M; // binds to I1.M (method)

interface I1 { static void M() { } }
interface I2 { static int M => 0;   }
interface I3 { static int M = 0;   }
interface I : I1, I2, I3 { }
```

```csharp
I i = null;
var x = i.M; // binds to I1.M (method)
x();

System.Action y = i.M; // binds to I1.M (method)

interface I1 { void M() { } }
interface I2 { int M => 0;   }
interface I : I1, I2 { }
```
