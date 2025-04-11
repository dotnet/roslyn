# Microsoft.CodeAnalysis.BannedApiAnalyzers

## [RS0030](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md): Do not use banned APIs

The symbol has been marked as banned in this project, and an alternate should be used instead.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS0031](https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md): The list of banned symbols contains a duplicate

The list of banned symbols contains a duplicate.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0035: External access to internal symbols outside the restricted namespace(s) is prohibited

RestrictedInternalsVisibleToAttribute enables a restricted version of InternalsVisibleToAttribute that limits access to internal symbols to those within specified namespaces. Each referencing assembly can only access internal symbols defined in the restricted namespaces that the referenced assembly allows.

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Error|
|CodeFix|False|
---
