# How to use Microsoft.CodeAnalysis.PublicApiAnalyzers

The following files have to be added to any project referencing this package to enable analysis:

- `PublicAPI.Shipped.txt`
- `PublicAPI.Unshipped.txt`

This can be done by:

- In Visual Studio, right-click the project in Solution Explorer, choose "Add -> New Item...", and then select "Text File" in the "Add New Item" dialog. Then right-click each file, select "Properties", and choose "C# analyzer additional file" for "Build Action" in the "Properties" window.
- Or, create these two files at the location you desire, then add the following text to your project/target file (replace file path with its actual location):

```xml
  <ItemGroup>
    <AdditionalFiles Include="PublicAPI.Shipped.txt" />
    <AdditionalFiles Include="PublicAPI.Unshipped.txt" />
  </ItemGroup>
```

## Nullable reference type support

To enable support for [nullable reference types](https://docs.microsoft.com/dotnet/csharp/nullable-references) add the following at the top of each `PublicAPI.*.txt` file:

```xml
#nullable enable
```

## Conditional API Differences

Sometimes APIs vary by compilation symbol such as target framework.

For example when using the [`#if` preprocessor directive](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if):

```c#
        public void Foo(string s)
        {}

#if NETCOREAPP3_0
        public void Foo(ReadOnlySpan<char> s)
        {}
#else
```

To correctly model the API differences between target frameworks (or any other property), use multiple instances of the `PublicAPI.*.txt` files.

If you have multiple target frameworks and APIs differ between them, use the following in your project file:

```xml
  <ItemGroup>
    <AdditionalFiles Include="PublicAPI/$(TargetFramework)/PublicAPI.Shipped.txt" />
    <AdditionalFiles Include="PublicAPI/$(TargetFramework)/PublicAPI.Unshipped.txt" />
  </ItemGroup>
```
