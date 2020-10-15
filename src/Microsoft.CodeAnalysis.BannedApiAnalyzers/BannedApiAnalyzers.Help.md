# How to use Microsoft.CodeAnalysis.BannedApiAnalyzers

The following file have to be added to any project referencing this package to enable analysis:

- BannedSymbols.txt

This can be done by:

- In Visual Studio, right click project in Solution Explorer, and choose "Add -> New Items", then select "Text File" in "Add new item" dialog.
- Or, create the file at the location you desire, then add the following text to your project/target file (replace file path with its actual location):

  ```xml
  <ItemGroup>
    <AdditionalFiles Include="BannedSymbols.txt" />
  </ItemGroup>
  ```

To add a symbol to banned list, just add an entry in the format below to the BannedSymbols.txt (Description Text will be displayed as description in diagnostics, which is optional):

```txt
{Documentation Comment ID string for the symbol}[;Description Text]
```

For details on ID string format, please refer to ["Documentation comments"](https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format).

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
