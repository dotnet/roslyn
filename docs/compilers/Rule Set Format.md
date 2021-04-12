Introduction
============

This document, in conjunction with the [Rule Set Schema](..//..//src//Compilers//Core//Portable//RuleSet//RuleSetSchema.xsd), describes the structure of .ruleset files used by the C# and Visual Basic compilers to turn diagnostic analyzers on and off and to control their severity.

This document only discusses the required and common parts of .ruleset files; for a full set, please see the schema file.

Sample
=====

The following demonstrates a small but complete example of a .ruleset file.

``` XML
<RuleSet Name="Project WizBang Rules"
         ToolsVersion="12.0">
  <Include Path="..\OtherRules.ruleset" 
           Action="Default" />
  <Rules AnalyzerId="System.Runtime.Analyzers"
         RuleNamespace="System.Runtime.Analyzers">
    <Rule Id="CA1027" Action="Warning" />
    <Rule Id="CA1309" Action="Error" />
    <Rule Id="CA2217" Action="Warning" />
  </Rules>
  <Rules AnalyzerId="System.Runtime.InteropServices.Analyzers"
         RuleNamespace="System.Runtime.InteropService.Analyzers">
    <Rule Id="CA1401" Action="None" />
    <Rule Id="CA2101" Action="Error" />
  </Rules>
</RuleSet>
```

Passing Rule Sets to the Compiler
=================================

Command Line
------------

Rule set files can be passed to csc.exe and vbc.exe using the `/ruleset` switch, which takes an absolute or relative file path. For example:
```
vbc.exe /ruleset:ProjectRules.ruleset ...
vbc.exe /ruleset:..\..\..\SolutionRules.ruleset ...
vbc.exe /ruleset:D:\WizbangCorp\Development\CompanyRules.ruleset ...
```

MSBuild Projects
----------------

Within MSBuild project files the rule set can be specified via the `CodeAnalysisRuleSet` property. For example:
``` XML
<PropertyGroup>
  <CodeAnalysisRuleSet>ProjectRules.ruleset</CodeAnalysisRuleSet>
</PropertyGroup>
```

Note that because the rule set is specified via a *property* rather than an *item*, IDEs like Visual Studio will not show the rule set as a file in your project by default. For this reason it is common to explicitly include the file as an item as well:
``` XML
<ItemGroup>
  <None Include="ProjectRules.ruleset" />
</ItemGroup>
```

Elements
========

`RuleSet`
---------

The `RuleSet` is the root element of the file.

Attributes:
* Name (**required**): A short, descriptive name for the file.
* Description (**optional**): A longer description of the purpose of the rule set file.
* ToolsVersion (**required**): This attribute is required for backwards compatibility with other tools that use .ruleset files. In practice it is the version of Visual Studio that produced this .ruleset file. When in doubt, simply use "12.0".

Children: `Include`, `Rules`

`Include`
---------

Pulls in the settings from the specified rule set file. Settings in the current file override settings in the included file.

Parent: `RuleSet`

Attributes:
* Path (**required**): An absolute or relative path to another .ruleset file.
* Action (**required**): Specifies the effective action of the included rules. Must be one of the following values:
 * Default - The rules use the actions specified in the included file.
 * Error - The included rules are treated as though their action values were all "Error".
 * Warning - The included rules are treated as though their actions values were all "Warning".
 * Info - The included rules are treated as though their actions values were all "Info".
 * Hidden - The included rules are treated as though their actions values were all "Hidden".
 * None - The included rules are treated as though their actions values were all "None".

Children: None.

`Rules`
-------

Hold settings for rules from a single diagnostic analyzer.

Parent: `RuleSet`

Attributes:
* AnalyzerId (**required**): The name of the analyzer. In practice this is the simple name of the assembly containing the diagnostics.
* RuleNamespace (**required**): This attribute is required for backwards compatibility with other tools that use .ruleset files. In practice it is generally the same as the AnalyzerId.

Children: `Rule`

`Rule`
------

Specifies what action to take for a given diagnostic rule.

Parent: `Rules`

Attributes:
* Id (**required**): The ID of the diagnostic rule.
* Action (**required**): One of the following values:
 * Error - Instances of this diagnostic are treated as compiler errors.
 * Warning - Instances of this diagnostic are treated as compiler warnings.
 * Info - Instances of this diagnostic are treated as compiler messages.
 * Hidden - Instances of this diagnostic are hidden from the user.
 * None - Turns off the diagnostic rule.

Children: None.

Notes:

Within a `Rules` element, a `Rule` with a given `Id` may only appear once. The C# and Visual Basic compilers impose a further constraint: if multiple `Rules` elements contain a `Rule` with the same `Id` they must all specify the same `Action` value.

The difference between the Hidden and None actions is subtle but important. When a diagnostic rule is set to Hidden instances of that rule are still created, but aren't displayed to the user by default. However, the host process can access these diagnostics and take some host-specific action. For example, Visual Studio uses a combination of Hidden diagnostics and a custom editor extension to "grey out" dead or unnecessary code in the editor. A diagnostic rule set to None, however, is effectively turned off. Instances of this diagnostic may not be produced at all; even if they are suppressed rather than being made available to the host.
