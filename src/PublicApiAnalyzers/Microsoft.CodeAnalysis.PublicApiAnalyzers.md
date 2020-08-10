# Microsoft.CodeAnalysis.PublicApiAnalyzers

## [RS0016](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Add public types and members to the declared API

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

All public types and members should be declared in PublicAPI.txt. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

## [RS0017](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Remove deleted types and members from the declared API

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

When removing a public type or member the corresponding entry in PublicAPI.txt should also be removed. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

## [RS0022](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Constructor make noninheritable base class inheritable

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Constructor makes its noninheritable base class inheritable, thereby exposing its protected members.

## [RS0024](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): The contents of the public API files are invalid

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

The contents of the public API files are invalid: {0}

## [RS0025](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Do not duplicate symbols in public API files

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

The symbol '{0}' appears more than once in the public API files.

## [RS0026](https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md): Do not add multiple public overloads with optional parameters

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Symbol '{0}' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See '{1}' for details.

## [RS0027](https://github.com/dotnet/roslyn/blob/master/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md): Public API with optional parameter(s) should have the most parameters amongst its public overloads.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Symbol '{0}' violates the backcompat requirement: 'Public API with optional parameter(s) should have the most parameters amongst its public overloads'. See '{1}' for details.

## [RS0036](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Annotate nullability of public types and members in the declared API

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

All public types and members should be declared with nullability annotations in PublicAPI.txt. This draws attention to API nullability changes in the code reviews and source control history, and helps prevent breaking changes.

## [RS0037](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Enable tracking of nullability of reference types in the declared API

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

PublicAPI.txt files should have `#nullable enable` to track nullability information, or this diagnostic should be suppressed. With nullability enabled, PublicAPI.txt records which types are nullable (suffix `?` on type) or non-nullable (suffix `!`). It also tracks any API that is still using an oblivious reference type (prefix `~` on line).

## [RS0041](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Public members should not use oblivious types

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

All public members should use either nullable or non-nullable reference types, but no oblivious reference types.

## [RS0048](https://github.com/dotnet/roslyn-analyzers/blob/master/src/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Missing shipped or unshipped public API file

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Public API file '{0}' is missing or not marked as an additional analyzer file

