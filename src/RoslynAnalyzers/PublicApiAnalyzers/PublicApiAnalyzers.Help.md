# How to use Microsoft.CodeAnalysis.PublicApiAnalyzers

To get started with the Public API Analyzer:

1. Add a package reference to [Microsoft.CodeAnalysis.PublicApiAnalyzers](https://www.nuget.org/packages/Microsoft.CodeAnalysis.PublicApiAnalyzers) to your project.
2. You will have `RS0016` diagnostics on all your public APIs.
3. Invoke the codefix on any `RS0016` to add the public APIs to the documented set. You can apply the codefix across the entire project or solution to easily document all APIs at once. Text files `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` will be added to each project in scope, if they do not already exist.

**Configuration:** If you would prefer the public API analyzer to bail out silently for projects with missing public API files, you can do so by setting the following .editorconfig option:

```ini
[*.cs]
dotnet_public_api_analyzer.require_api_files = true
```

See [Configuration options for code analysis](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options) for more details on how to setup editorconfig based configuration.

## Package version earlier than 3.3.x

If you are using a `Microsoft.CodeAnalysis.PublicApiAnalyzers` package with version prior to 3.3.x, then you will need to manually create the following public API files in each project directory that needs to be analyzed. Additionally, you will need to mark the above files as analyzer additional files to enable analysis.

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

To enable support for [nullable reference types](https://learn.microsoft.com/dotnet/csharp/nullable-references), make sure that you are using a Roslyn compiler version 3.5 (or newer) in your build process and then add the following at the top of each `PublicAPI.*.txt` file:

```xml
#nullable enable
```

One way of checking the version of your compiler is to add `#error version` in a source file, then looking at the error message output in your build logs.

At that point, reference types in annotated code will need to be annotated with either a `?` (nullable) or a `!` (non-nullable). For instance, `C.AnnotatedMethod(string! nonNullableParameter, string? nullableParameter, int valueTypeParameter) -> void`.

Any public API that haven't been annotated (i.e. uses an oblivious reference type) will be tracked with a `~` marker. The marker lets you track how many public APIs still lack annotations. For instance, `~C.ObliviousMethod() -> string`.

We recommend to enable [RS0041 warning](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/Microsoft.CodeAnalysis.PublicApiAnalyzers.md) if you start with a fresh project or your project has reached 100% annotation on your public API to ensure that all public APIs remain annotated.
If you are in the process of annotating an existing project, we recommended to disable this warning until you complete the annotation. The rule can be disabled via `.editorconfig` with `dotnet_diagnostic.RS0041.severity = none`.

## Conditional API Differences

Sometimes APIs vary by compilation symbol such as target framework.

For example when using the [`#if` preprocessor directive](https://learn.microsoft.com/dotnet/csharp/language-reference/preprocessor-directives/preprocessor-if):

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
