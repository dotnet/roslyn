This is forked from [01c6dd2bf7dc509de289da60b78df8440ce4c11d@Microsoft/RoslynClrHeapAllocationAnalyzer](https://github.com/Microsoft/RoslynClrHeapAllocationAnalyzer/commit/01c6dd2bf7dc509de289da60b78df8440ce4c11d)

How to avoid conflicts of `PerformanceSensitiveAttribute` in projects contain `InternalsVisibleTo` (IVT)
--------------------------------

Because we inject the source of `PerformanceSensitiveAttribute` into projects referencing PerformanceSensitive analyzer package by default, which is declared as `internal`, you may run into warning CS0436 if you have IVTs defined in your projects.

One way to resolve this issue is setting `GeneratePerformanceSensitiveAttribute` property to true in the project at the root of your IVT tree, and false otherwise.

For example, given the dependency graph below, if project A has IVT for project B, and project C has IVT for project D and project E. You need to set `GeneratePerformanceSensitiveAttribute` to true in A and C, and false in B, D and E.

```text
     A
     |
    / \
   B   C
      / \
     D   E
```

Internally, this is implemented in the `Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers.targets` file included in the nuget package.

```xml
<Project>

  <PropertyGroup>
    <GeneratePerformanceSensitiveAttribute Condition="'$(GeneratePerformanceSensitiveAttribute)' == ''">true</GeneratePerformanceSensitiveAttribute>
    <PerformanceSensitiveAttributePath Condition="'$(PerformanceSensitiveAttributePath)' == ''">$(MSBuildThisFileDirectory)PerformanceSensitiveAttribute$(DefaultLanguageSourceExtension)</PerformanceSensitiveAttributePath>
  </PropertyGroup>

  <ItemGroup Condition="'$(GeneratePerformanceSensitiveAttribute)' == 'true' and Exists($(PerformanceSensitiveAttributePath))">
    <Compile Include="$(PerformanceSensitiveAttributePath)" Visible="false" />

    <!-- Make sure the source file is embedded in PDB to support Source Link -->
    <EmbeddedFiles Condition="'$(DebugType)' != 'none'" Include="$(PerformanceSensitiveAttributePath)" />
  </ItemGroup>

</Project>
```
