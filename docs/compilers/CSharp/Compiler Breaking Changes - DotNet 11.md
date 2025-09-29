# Breaking changes in Roslyn after .NET 10.0.100 through .NET 11 (version number tbd)

This document lists known breaking changes in Roslyn after .NET 10 general release (.NET SDK version 10.0.100) through .NET 11 general release (.NET SDK version TBD).

## `with()` as a collection expression element is treated as collection construction *arguments*

***Introduced in Visual Studio 2022 version TBD***

`with(...)` when used as an element in a collection expression is bound as arguments passed to constructor or
factory method used to create the collection, rather than as an invocation expression of a method named `with`.

To bind to a method named `with`, use `@with` instead.

```cs
object x, y, z = ...;
object[] items;

items = [with(x, y), z];  // C#13: call to with() method; C#14: error args not supported for object[]
items = [@with(x, y), z]; // call to with() method
object with(object a, object b) { ... }
```