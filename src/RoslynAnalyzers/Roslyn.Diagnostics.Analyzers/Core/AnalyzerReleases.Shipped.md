## Release 2.9.8

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0001 | RoslyDiagnosticsPerformance | Warning | SpecializedEnumerableCreationAnalyzer
RS0002 | RoslyDiagnosticsPerformance | Warning | SpecializedEnumerableCreationAnalyzer
RS0005 | RoslyDiagnosticsPerformance | Warning | CodeActionCreateAnalyzer
RS0006 | RoslyDiagnosticsReliability | Warning | DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer
RS0013 | RoslyDiagnosticsPerformance | Disabled | DiagnosticDescriptorAccessAnalyzer
RS0019 | RoslyDiagnosticsReliability | Disabled | SymbolDeclaredEventAnalyzer
RS0023 | RoslyDiagnosticsReliability | Warning | PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer
RS0032 | RoslyDiagnosticsReliability | Disabled | TestExportsShouldNotBeDiscoverable
RS0033 | RoslyDiagnosticsReliability | Warning | ImportingConstructorShouldBeObsolete
RS0034 | RoslyDiagnosticsReliability | Warning | ExportedPartsShouldHaveImportingConstructor

## Release 3.3.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0040 | RoslynDiagnosticsReliability | Warning | DefaultableTypeShouldHaveDefaultableFieldsAnalyzer
RS0042 | RoslynDiagnosticsReliability | Warning | DoNotCopyValue
RS0043 | RoslynDiagnosticsMaintainability | Warning | DoNotCallGetTestAccessor
RS0101 | RoslynDiagnosticsMaintainability | Warning | AbstractBlankLinesDiagnosticAnalyzer

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
RS0001 | RoslynDiagnosticsPerformance | Warning | RoslyDiagnosticsPerformance | Warning | SpecializedEnumerableCreationAnalyzer
RS0002 | RoslynDiagnosticsPerformance | Warning | RoslyDiagnosticsPerformance | Warning | SpecializedEnumerableCreationAnalyzer
RS0005 | RoslynDiagnosticsPerformance | Warning | RoslyDiagnosticsPerformance | Warning | CodeActionCreateAnalyzer
RS0006 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer
RS0019 | RoslynDiagnosticsReliability | Disabled | RoslyDiagnosticsReliability | Disabled | SymbolDeclaredEventAnalyzer
RS0023 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer
RS0032 | RoslynDiagnosticsReliability | Disabled | RoslyDiagnosticsReliability | Disabled | TestExportsShouldNotBeDiscoverable
RS0033 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | ImportingConstructorShouldBeObsolete
RS0034 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | ExportedPartsShouldHaveImportingConstructor

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0013 | RoslyDiagnosticsPerformance | Disabled | DiagnosticDescriptorAccessAnalyzer

## Release 3.3.3

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0049 | RoslynDiagnosticsReliability | Warning | TemporaryArrayAsRefAnalyzer

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0101 | RoslynDiagnosticsMaintainability | Warning | AbstractBlankLinesDiagnosticAnalyzer

## Release 3.3.4

### Removed Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0005 | RoslyDiagnosticsPerformance | Warning | CodeActionCreateAnalyzer
