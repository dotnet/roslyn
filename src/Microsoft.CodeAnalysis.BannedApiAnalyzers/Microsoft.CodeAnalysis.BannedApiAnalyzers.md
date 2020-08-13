# Microsoft.CodeAnalysis.BannedApiAnalyzers

## [RS0030](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md): Do not used banned APIs

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

The symbol has been marked as banned in this project, and an alternate should be used instead.

## [RS0031](https://github.com/dotnet/roslyn-analyzers/blob/master/src/Microsoft.CodeAnalysis.BannedApiAnalyzers/BannedApiAnalyzers.Help.md): The list of banned symbols contains a duplicate

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

The list of banned symbols contains a duplicate.

## RS0035: External access to internal symbols outside the restricted namespace(s) is prohibited

|Item|Value|
|-|-|
|Category|ApiDesign|
|Enabled|True|
|Severity|Error|
|CodeFix|False|

### Rule description

RestrictedInternalsVisibleToAttribute enables a restricted version of InternalsVisibleToAttribute that limits access to internal symbols to those within specified namespaces. Each referencing assembly can only access internal symbols defined in the restricted namespaces that the referenced assembly allows.

