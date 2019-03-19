How to use Microsoft.CodeAnalysis.BannedApiAnalyzers
--------------------------------

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

To add a symbol to banned list, just add an enrty in the format below to the BannedSymbols.txt (Please note that things enclosed in square brackets are optional, and Description Text defined in the entry will be part of the diagnostic description):

        {Symbol Declaration Comment ID}[;Description Text]

_Symbol Declaration Comment ID_ :  
_{Kind Character}:{Symbol Declaration Comment ID Name}_

Supported Kind Character:
- E: event  
- F: field  
- M: method  
- P: property  
- T: named type  



Examples of banned API entries for symbols declared in the source below:

```cs
namespace N
{
    class BannedType
    {
        public BannedType() {}

        public int BannedMethod() {}

        public void BannedMethod(int i) {}

        public void BannedMethod<T>(T t) {}
    }
}
```

| Symbol in Source                      | Metadata Name |
| -----------                           | ----------- |
| `class BannedType`                    | `T:N.BannedType`       |
| `BannedType()`                        | `M:N.BannedType.#ctor`       |
| `int BannedMethod()`                  | `M:N.BannedType.BannedMethod~System.Int32`       |
| `void BannedMethod(int i)`            | `M:N.BannedType.BannedMethod(System.Int32)`       |
| `void BannedMethod<T>(T t)`           | ``M:N.BannedType.BannedMethod`1(`0)``       |

For more details on how Symbol Declaration Comment ID is created, please refer to [DocumentationCommentId](http://source.roslyn.io/#Microsoft.CodeAnalysis/DocumentationCommentId.cs,483141c2cbc3eaa6).

Rules
--------------------------------
### RS0030: Do not use banned APIs ###

Category: ApiDesign

Severity: Warning

### RS0031: The list of banned symbols contains a duplicate ###

Category: ApiDesign

Severity: Warning