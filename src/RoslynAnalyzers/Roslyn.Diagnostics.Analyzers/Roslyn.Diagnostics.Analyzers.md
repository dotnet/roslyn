# Roslyn.Diagnostics.Analyzers

## RS0001: Use 'SpecializedCollections.EmptyEnumerable()'

Use 'SpecializedCollections.EmptyEnumerable()'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0002: Use 'SpecializedCollections.SingletonEnumerable()'

Use 'SpecializedCollections.SingletonEnumerable()'

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0004: Invoke the correct property to ensure correct use site diagnostics

Invoke the correct property to ensure correct use site diagnostics

|Item|Value|
|-|-|
|Category|Usage|
|Enabled|False|
|Severity|Error|
|CodeFix|False|
---

## RS0006: Do not mix attributes from different versions of MEF

Do not mix attributes from different versions of MEF.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0019: 'SymbolDeclaredEvent' must be generated for source symbols

Compilation event queue is required to generate symbol declared events for all declared source symbols. Hence, every source symbol type or one of its base types must generate a symbol declared event.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|False|
|Severity|Error|
|CodeFix|False|
---

## RS0023: Parts exported with MEFv2 must be marked with 'SharedAttribute'

Part exported with MEFv2 must be marked with the 'SharedAttribute'.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS0032: Test exports should not be discoverable

Test exports should not be discoverable.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|False|
|Severity|Warning|
|CodeFix|True|
---

## RS0033: Importing constructor should be marked with 'ObsoleteAttribute'

Importing constructor should be marked with 'ObsoleteAttribute'.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS0034: Exported parts should have a public constructor marked with 'ImportingConstructorAttribute'

Exported parts should be marked with 'ImportingConstructorAttribute'.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS0038: Prefer null literal

Use 'null' instead of 'default' for nullable types.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS0040: Defaultable types should have defaultable fields

Defaultable types should have defaultable fields.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0042: Do not copy value

Auto-properties always copy values, so they cannot be declared with non-copyable types.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0043: Do not call 'GetTestAccessor()'

'GetTestAccessor()' is a helper method reserved for testing. Production code must not call this member.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0046: Avoid the 'Opt' suffix

Avoid the 'Opt' suffix in a nullable-enabled code.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS0049: Instance of TemporaryArray\<T>.AsRef() must be a 'using' variable

Instance of TemporaryArray\<T>.AsRef() must be a 'using' variable.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsReliability|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS0062: Do not capture primary constructor parameters

Primary constructor parameters should not be implicitly captured. Manually assign them to fields at the start of the type.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsMaintainability|
|Enabled|False|
|Severity|Error|
|CodeFix|False|
---

## RS0063: Do not use interpolated strings with 'Debug.Assert'

'Debug.Assert' on .NET Framework eagerly creates the string value. This can cause OOMs in tests, particularly for strings that involve syntax nodes. Use 'RoslynDebug.Assert' instead, which will only create the string if required.

|Item|Value|
|-|-|
|Category|RoslynDiagnosticsPerformance|
|Enabled|False|
|Severity|Warning|
|CodeFix|True|
---
