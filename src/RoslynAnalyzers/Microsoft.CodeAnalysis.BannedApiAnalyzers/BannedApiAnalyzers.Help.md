# How to use Microsoft.CodeAnalysis.BannedApiAnalyzers

The following file or files have to be added to any project referencing this package to enable analysis:

- BannedSymbols.txt
- BannedSymbols.\*.txt

This can be done by:

- In Visual Studio, right click project in Solution Explorer, and choose "Add -> New Items", then select "Text File" in "Add new item" dialog.
- Or, create the file at the location you desire, then add the following text to your project/target file (replace file path with its actual location):

  ```xml
  <ItemGroup>
    <AdditionalFiles Include="BannedSymbols.txt" />
  </ItemGroup>
  ```

To add a symbol to the banned list, just add an entry in the format below to one of the configuration files (Description Text will be displayed as description in diagnostics, which is optional):

```txt
{Documentation Comment ID string for the symbol}[;Description Text]
```

Comments can be indicated with `//`, in the same way that they work in C#.

For details on ID string format, please refer to ["ID string format"](https://github.com/dotnet/csharpstandard/blob/standard-v6/standard/documentation-comments.md#d42-id-string-format).

Examples of BannedSymbols.txt entries for symbols declared in the source below:

```cs
namespace N
{
    class BannedType
    {
        public BannedType() {}

        public int BannedMethod() {}

        public void BannedMethod(int i) {}

        public void BannedMethod<T>(T t) {}

        public void BannedMethod<T>(Func<T> f) {}

        public string BannedField;

        public string BannedProperty { get; }

        public event EventHandler BannedEvent;
    }

    class BannedType<T>
    {
    }
}
```

| Symbol in Source                      | Sample Entry in BannedSymbols.txt
| -----------                           | -----------
| `class BannedType`                    | `T:N.BannedType;Don't use BannedType`
| `class BannedType<T>`                 | ``T:N.BannedType`1;Don't use BannedType<T>``
| `BannedType()`                        | `M:N.BannedType.#ctor`
| `int BannedMethod()`                  | `M:N.BannedType.BannedMethod`
| `void BannedMethod(int i)`            | `M:N.BannedType.BannedMethod(System.Int32);Don't use BannedMethod`
| `void BannedMethod<T>(T t)`           | ```M:N.BannedType.BannedMethod`1(``0)```
| `void BannedMethod<T>(Func<T> f)`     | ```M:N.BannedType.BannedMethod`1(System.Func{``0})```
| `string BannedField`                  | `F:N.BannedType.BannedField`
| `string BannedProperty { get; }`      | `P:N.BannedType.BannedProperty`
| `event EventHandler BannedEvent;`     | `E:N.BannedType.BannedEvent`
| `namespace N`                         | `N:N`

## Configuration

The BannedApiAnalyzer supports configuration via `.editorconfig` files.

### Exclude Generated Code

You can exclude source-generated files from analysis by adding the following to your `.editorconfig`:

```ini
exclude_generated_code = true
```

By default, the analyzer runs on all files, including generated code. Setting this option to `true` will skip analysis of files that are marked as generated code.
