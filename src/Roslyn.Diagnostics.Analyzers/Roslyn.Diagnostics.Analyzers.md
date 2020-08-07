# Roslyn.Diagnostics.Analyzers

## RS0001: Use 'SpecializedCollections.EmptyEnumerable()'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Use 'SpecializedCollections.EmptyEnumerable()'

## RS0002: Use 'SpecializedCollections.SingletonEnumerable()'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Use 'SpecializedCollections.SingletonEnumerable()'

## RS0004: Invoke the correct property to ensure correct use site diagnostics

|Item|Value|
|-|-|
|Category|Usage|
|Enabled|False|
|Severity|Error|
|CodeFix|False|

### Rule description

Invoke the correct property to ensure correct use site diagnostics

## RS0005: Do not use generic 'CodeAction.Create' to create 'CodeAction'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Do not use generic 'CodeAction.Create' to create 'CodeAction'

## RS0006: Do not mix attributes from different versions of MEF

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Do not mix attributes from different versions of MEF.

## RS0019: 'SymbolDeclaredEvent' must be generated for source symbols

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|False|
|Severity|Error|
|CodeFix|False|

### Rule description

Compilation event queue is required to generate symbol declared events for all declared source symbols. Hence, every source symbol type or one of its base types must generate a symbol declared event.

## RS0023: Parts exported with MEFv2 must be marked with 'SharedAttribute'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Part exported with MEFv2 must be marked with the 'SharedAttribute'.

## RS0032: Test exports should not be discoverable

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|False|
|Severity|Warning|
|CodeFix|True|

### Rule description

Test exports should not be discoverable.

## RS0033: Importing constructor should be marked with 'ObsoleteAttribute'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Importing constructor should be marked with 'ObsoleteAttribute'.

## RS0034: Exported parts should be marked with 'ImportingConstructorAttribute'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Exported parts should be marked with 'ImportingConstructorAttribute'.

## RS0038: Prefer null literal

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Use 'null' instead of 'default' for nullable types.

## RS0040: Defaultable types should have defaultable fields

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Defaultable types should have defaultable fields.

## RS0042: Do not copy value

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

Do not unbox non-copyable value types.

## RS0043: Do not call 'GetTestAccessor()'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|

### Rule description

'GetTestAccessor()' is a helper method reserved for testing. Production code must not call this member.

## RS0046: Avoid the 'Opt' suffix

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Avoid the 'Opt' suffix in a nullable-enabled code.

## RS0100: Statements must be placed on their own line

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Statements must be placed on their own line

## RS0101: Avoid multiple blank lines

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Avoid multiple blank lines

## RS0102: Braces must not have blank lines between them

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|

### Rule description

Braces must not have blank lines between them

