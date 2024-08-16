# This document lists known breaking changes in Roslyn after .NET 8 all the way to .NET 9.


## InlineArray attribute on a record struct type is no longer allowed.

***Introduced in Visual Studio 2022 version 17.11***

```cs
[System.Runtime.CompilerServices.InlineArray(10)] // error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
record struct Buffer1()
{
    private int _element0;
}

[System.Runtime.CompilerServices.InlineArray(10)] // error CS9259: Attribute 'System.Runtime.CompilerServices.InlineArray' cannot be applied to a record struct.
record struct Buffer2(int p1)
{
}
```


## Iterators introduce safe context in C# 13 and newer

***Introduced in Visual Studio 2022 version 17.11***

Although the language spec states that iterators introduce a safe context, Roslyn does not implement that in C# 12 and lower.
This will change in C# 13 as part of [a feature which allows unsafe code in iterators](https://github.com/dotnet/roslyn/issues/72662).
The change does not break normal scenarios as it was disallowed to use unsafe constructs directly in iterators anyway.
However, it can break scenarios where an unsafe context was previously inherited into nested local functions, for example:

```cs
unsafe class C // unsafe context
{
    System.Collections.Generic.IEnumerable<int> M() // an iterator
    {
        yield return 1;
        local();
        void local()
        {
            int* p = null; // allowed in C# 12; error in C# 13
        }
    }
}
```

You can work around the break simply by adding the `unsafe` modifier to the local function.

## .editorconfig values no longer support trailing comments

***Introduced in Visual Studio 2022 version 17.[TBD]***

The compiler is updated based on the EditorConfig specification clarification in
[editorconfig/specification#31](https://github.com/editorconfig/specification/pull/31). Following this change, comments
in **.editorconfig** and **.globalconfig** files must now appear on their own line. Comments which appear at the end of
a property value are now treated as part of the property value itself. This changes the way values are passed to
analyzers for lines with the following form:

```ini
[*.cs]
key = value # text
key2 = value2 ; text2
```

The following table shows how this change affects values passed to analyzers:

| EditorConfig line       | New compiler interpretation | Old interpretation |
| ----------------------- | --------------------------- | ------------------ |
| `key = value # text`    | `value # text`              | `value`            |
| `key2 = value2 ; text2` | `value2 ; text2`            | `value2`           |
