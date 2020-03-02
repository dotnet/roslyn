
Rule ID | Title | Category | Enabled | Severity | CodeFix | Description |
--------|-------|----------|---------|----------|---------|--------------------------------------------------------------------------------------------------------------|
RS0001 | Use SpecializedCollections.EmptyEnumerable() | RoslynDiagnosticsPerformance | True | Warning | False | Use SpecializedCollections.EmptyEnumerable() |
RS0002 | Use SpecializedCollections.SingletonEnumerable() | RoslynDiagnosticsPerformance | True | Warning | False | Use SpecializedCollections.SingletonEnumerable() |
RS0004 | Invoke the correct property to ensure correct use site diagnostics. | Usage | False | Error | False | Invoke the correct property to ensure correct use site diagnostics. |
RS0005 | Do not use generic CodeAction.Create to create CodeAction | RoslynDiagnosticsPerformance | True | Warning | False | Do not use generic CodeAction.Create to create CodeAction |
RS0006 | Do not mix attributes from different versions of MEF | RoslynDiagnosticsReliability | True | Warning | False | Do not mix attributes from different versions of MEF |
RS0013 | Do not invoke Diagnostic.Descriptor | RoslynDiagnosticsPerformance | False | Warning | False | Accessing the Descriptor property of Diagnostic in compiler layer leads to unnecessary string allocations for fields of the descriptor that are not utilized in command line compilation. Hence, you should avoid accessing the Descriptor of the compiler diagnostics here. Instead you should directly access these properties off the Diagnostic type. |
RS0019 | SymbolDeclaredEvent must be generated for source symbols | RoslynDiagnosticsReliability | False | Error | False | Compilation event queue is required to generate symbol declared events for all declared source symbols. Hence, every source symbol type or one of its base types must generate a symbol declared event. |
RS0023 | Parts exported with MEFv2 must be marked as Shared | RoslynDiagnosticsReliability | True | Warning | False | Part exported with MEFv2 must be marked with the Shared attribute. |
RS0032 | Test exports should not be discoverable | RoslynDiagnosticsReliability | False | Warning | True | Test exports should not be discoverable |
RS0033 | Importing constructor should be [Obsolete] | RoslynDiagnosticsReliability | True | Warning | True | Importing constructor should be [Obsolete] |
RS0034 | Exported parts should have [ImportingConstructor] | RoslynDiagnosticsReliability | True | Warning | True | Exported parts should have [ImportingConstructor] |
