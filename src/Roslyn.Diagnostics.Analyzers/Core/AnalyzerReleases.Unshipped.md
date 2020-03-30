### New Rules
Rule ID | Category | Severity | Notes
--------|----------|----------|-------
RS0040 | RoslynDiagnosticsReliability | Warning | DefaultableTypeShouldHaveDefaultableFieldsAnalyzer
RS0042 | RoslynDiagnosticsReliability | Warning | DoNotCopyValue

### Changed Rules
Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
RS0001 | RoslynDiagnosticsPerformance | Warning | RoslyDiagnosticsPerformance | Warning | SpecializedEnumerableCreationAnalyzer
RS0002 | RoslynDiagnosticsPerformance | Warning | RoslyDiagnosticsPerformance | Warning | SpecializedEnumerableCreationAnalyzer
RS0005 | RoslynDiagnosticsPerformance | Warning | RoslyDiagnosticsPerformance | Warning | CodeActionCreateAnalyzer
RS0006 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer
RS0013 | RoslynDiagnosticsPerformance | Disabled | RoslyDiagnosticsPerformance | Disabled | DiagnosticDescriptorAccessAnalyzer
RS0019 | RoslynDiagnosticsReliability | Disabled | RoslyDiagnosticsReliability | Disabled | SymbolDeclaredEventAnalyzer
RS0023 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer
RS0032 | RoslynDiagnosticsReliability | Disabled | RoslyDiagnosticsReliability | Disabled | TestExportsShouldNotBeDiscoverable
RS0033 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | ImportingConstructorShouldBeObsolete
RS0034 | RoslynDiagnosticsReliability | Warning | RoslyDiagnosticsReliability | Warning | ExportedPartsShouldHaveImportingConstructor