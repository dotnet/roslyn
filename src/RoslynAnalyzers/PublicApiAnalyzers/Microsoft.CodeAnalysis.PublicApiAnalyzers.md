# Microsoft.CodeAnalysis.PublicApiAnalyzers

## [RS0016](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Add public types and members to the declared API

All public types and members should be declared in PublicAPI.txt. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## [RS0017](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Remove deleted types and members from the declared API

When removing a public type or member, put that entry in PublicAPI.Unshipped.txt with '\*REMOVED\*' prefix. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0022](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Constructor make noninheritable base class inheritable

Constructor makes its noninheritable base class inheritable, thereby exposing its protected members

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0024](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): The contents of the public API files are invalid

The contents of the public API files are invalid: {0}

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0025](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Do not duplicate symbols in public API files

The symbol '{0}' appears more than once in the public API files

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0026](https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md): Do not add multiple public overloads with optional parameters

Symbol '{0}' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See '{1}' for details.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0027](https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md): API with optional parameter(s) should have the most parameters amongst its public overloads

'{0}' violates the backcompat requirement: 'API with optional parameter(s) should have the most parameters amongst its public overloads'. See '{1}' for details.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0036](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Annotate nullability of public types and members in the declared API

All public types and members should be declared with nullability annotations in PublicAPI.txt. This draws attention to API nullability changes in the code reviews and source control history, and helps prevent breaking changes.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## [RS0037](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Enable tracking of nullability of reference types in the declared API

PublicAPI.txt files should have `#nullable enable` to track nullability information, or this diagnostic should be suppressed. With nullability enabled, PublicAPI.txt records which types are nullable (suffix `?` on type) or non-nullable (suffix `!`). It also tracks any API that is still using an oblivious reference type (prefix `~` on line).

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## [RS0041](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Public members should not use oblivious types

All public members should use either nullable or non-nullable reference types, but no oblivious reference types.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0048](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Missing shipped or unshipped public API file

Public API file '{0}' is missing or not marked as an additional analyzer file

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0050](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): API is marked as removed but it exists in source code

Symbol '{0}' is marked as removed but it isn't deleted in source code

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0051](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Add internal types and members to the declared API

All internal types and members should be declared in InternalAPI.txt. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|True|
---

## [RS0052](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Remove deleted types and members from the declared internal API

When removing a internal type or member, put that entry in InternalAPI.Unshipped.txt with '\*REMOVED\*' prefix. This draws attention to API changes in the code reviews and source control history, and helps prevent breaking changes.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0053](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): The contents of the internal API files are invalid

The contents of the internal API files are invalid: {0}

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0054](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Do not duplicate symbols in internal API files

The symbol '{0}' appears more than once in the internal API files

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0055](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Annotate nullability of internal types and members in the declared API

All internal types and members should be declared with nullability annotations in InternalAPI.txt. This draws attention to API nullability changes in the code reviews and source control history, and helps prevent breaking changes.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|True|
---

## [RS0056](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Enable tracking of nullability of reference types in the declared API

InternalAPI.txt files should have `#nullable enable` to track nullability information, or this diagnostic should be suppressed. With nullability enabled, InternalAPI.txt records which types are nullable (suffix `?` on type) or non-nullable (suffix `!`). It also tracks any API that is still using an oblivious reference type (prefix `~` on line).

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|True|
---

## [RS0057](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Internal members should not use oblivious types

All internal members should use either nullable or non-nullable reference types, but no oblivious reference types.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0058](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Missing shipped or unshipped internal API file

Internal API file '{0}' is missing or not marked as an additional analyzer file

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0059](https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md): Do not add multiple public overloads with optional parameters

Symbol '{0}' violates the backcompat requirement: 'Do not add multiple overloads with optional parameters'. See '{1}' for details.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0060](https://github.com/dotnet/roslyn/blob/main/docs/Adding%20Optional%20Parameters%20in%20Public%20API.md): API with optional parameter(s) should have the most parameters amongst its public overloads

'{0}' violates the backcompat requirement: 'API with optional parameter(s) should have the most parameters amongst its public overloads'. See '{1}' for details.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## [RS0061](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/PublicApiAnalyzers/PublicApiAnalyzers.Help.md): Constructor make noninheritable base class inheritable

Constructor makes its noninheritable base class inheritable, thereby exposing its protected members

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---
