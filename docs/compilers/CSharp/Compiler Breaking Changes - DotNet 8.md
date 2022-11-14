# This document lists known breaking changes in Roslyn after .NET 7 all the way to .NET 8.

## Inferred delegate type for methods includes default parameter values and `params` modifier

***Introduced in Visual Studio 2022 version 17.5***

In .NET SDK 7.0.100 or earlier, delegate types inferred from methods ignored default parameter values and `params` modifiers
as demonstrated in the following code:

```csharp
void Method(int i = 0, params int[] xs) { }
var action = Method; // System.Action<int, int[]>
DoAction(action, 1); // ok
void DoAction(System.Action<int, int[]> a, int p) => a(p, new[] { p });
```

In .NET SDK 7.0.200 or later, such methods are inferred as anonymous synthesized delegate types
with the same default parameter values and `params` modifiers.
This change can break the code above as demonstrated below:

```csharp
void Method(int i = 0, params int[] xs) { }
var action = Method; // delegate void <anonymous delegate>(int arg1 = 0, params int[] arg2)
DoAction(action, 1); // error CS1503: Argument 1: cannot convert from '<anonymous delegate>' to 'System.Action<int, int[]>'
void DoAction(System.Action<int, int[]> a, int p) => a(p, new[] { p });
```

You can learn more about this change in the associated [proposal](https://github.com/dotnet/csharplang/blob/main/proposals/lambda-method-group-defaults.md#breaking-change).
