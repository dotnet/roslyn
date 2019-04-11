How to use Microsoft.CodeAnalysis.PublicApiAnalyzers
--------------------------------

The following files have to be added to any project referencing this package to enable analysis:

- PublicAPI.Shipped.txt
- PublicAPI.Unshipped.txt

This can be done by:

- In Visual Studio, right click project in Solution Explorer, and choose "Add -> New Items", then select "Text File" in "Add new item" dialog.
- Or, create these two files at the location you desire, then add the following text to your project/target file (replace file path with its actual location):

```xml
  <ItemGroup>
    <AdditionalFiles Include="PublicAPI.Shipped.txt" />
    <AdditionalFiles Include="PublicAPI.Unshipped.txt" />
  </ItemGroup>
```