# Union patterns and null values

The [C# 15 unions proposal](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-15.0/unions.md#property-pattern)
distinguishes patterns that match a union instance from patterns that match its stored value.
This distinction also applies when the stored value is null.

For example:

```csharp
union NullableIntOrString(int?, string);

NullableIntOrString u = (int?)null;

Console.WriteLine(u is null);                  // True
Console.WriteLine(u is not null);              // False
Console.WriteLine(u is { });                   // True
Console.WriteLine(u is object { });            // False
Console.WriteLine(u is { Value: not null });   // False
```

A property pattern without a type, such as `{ }`, matches the union instance.
A non-nullable union struct always matches `{ }`, even when its `Value` is null.
For a class union or a nullable union struct, `{ }` tests whether the instance is non-null,
independently of its stored value. A designation such as `{ } captured` captures that instance.

The `null` pattern matches the stored value. For a class union or a nullable union struct,
it also matches a null instance. The `not null` pattern negates that test, so it succeeds
only when both the instance and its stored value are non-null.

To test the stored value with a property pattern, provide a type, as in `object { }`.
When the union exposes its `Value` property, `{ Value: not null }` explicitly matches that property.
The difference between `{ }` and `not null` is intentional for unions.
