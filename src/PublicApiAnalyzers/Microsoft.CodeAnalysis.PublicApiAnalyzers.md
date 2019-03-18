How to use Microsoft.CodeAnalysis.PublicApiAnalyzers
--------------------------------

The following files have to be added to any project referencing this package to enable analysis:

- PublicAPI.Shipped.txt
- PublicAPI.Unshipped.txt

This can be done by:

- In Visual Studio, right click project in Solution Explorer, and choose "Add -> New Items", then select "Text File" in "Add new item" dialog.
- Or, create these two files at the location you desire, then add the following text to your project file (replace the file path if they are not located at the project root directory):

```xml
  <ItemGroup>
    <AdditionalFiles Include="PublicAPI.Shipped.txt" />
    <AdditionalFiles Include="PublicAPI.Unshipped.txt" />
  </ItemGroup>
  ```


Rules
--------------------------------

### RS0016: Add public types and members to the declared API ###

All public types and members should be declared in PublicAPI.txt. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

Category: ApiDesign

Severity: Warning

### RS0017: Remove deleted types and members from the declared API ###

When removing a public type or member the corresponding entry in PublicAPI.txt should also be removed. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

Category: ApiDesign

Severity: Warning

### RS0022: Constructor make noninheritable base class inheritable ###

Category: ApiDesign

Severity: Warning

### RS0024: The contents of the public API files are invalid ###

Category: ApiDesign

Severity: Warning

### RS0025: Do not duplicate symbols in public API files ###

Category: ApiDesign

Severity: Warning

### RS0026: Do not add multiple overloads with optional parameters ###

https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md

Category: ApiDesign

Severity: Warning

### RS0027: Public API with optional parameter(s) should have the most parameters amongst its public overloads ###

https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md

Category: ApiDesign

Severity: Warning