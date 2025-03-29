# Rules without documentation

Rule ID | Missing Help Link | Title |
--------|-------------------|-------|
RS1001 |  | Missing diagnostic analyzer attribute |
RS1002 |  | Missing kind argument when registering an analyzer action |
RS1003 |  | Unsupported SymbolKind argument when registering a symbol analyzer action |
RS1004 |  | Recommend adding language support to diagnostic analyzer |
RS1005 |  | ReportDiagnostic invoked with an unsupported DiagnosticDescriptor |
RS1006 |  | Invalid type argument for DiagnosticAnalyzer's Register method |
RS1007 |  | Provide localizable arguments to diagnostic descriptor constructor |
RS1008 |  | Avoid storing per-compilation data into the fields of a diagnostic analyzer |
RS1009 |  | Only internal implementations of this interface are allowed |
RS1010 |  | Create code actions should have a unique EquivalenceKey for FixAll occurrences support |
RS1011 |  | Use code actions that have a unique EquivalenceKey for FixAll occurrences support |
RS1012 |  | Start action has no registered actions |
RS1013 |  | Start action has no registered non-end actions |
RS1014 |  | Do not ignore values returned by methods on immutable objects |
RS1015 |  | Provide non-null 'helpLinkUri' value to diagnostic descriptor constructor |
RS1016 |  | Code fix providers should provide FixAll support |
RS1017 |  | DiagnosticId for analyzers must be a non-null constant |
RS1018 |  | DiagnosticId for analyzers must be in specified format |
RS1019 |  | DiagnosticId must be unique across analyzers |
RS1020 |  | Category for analyzers must be from the specified values |
RS1021 |  | Invalid entry in analyzer category and diagnostic ID range specification file |
RS1022 | <https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1022.md> | Do not use types from Workspaces assembly in an analyzer |
RS1024 |  | Symbols should be compared for equality |
RS1025 |  | Configure generated code analysis |
RS1026 |  | Enable concurrent execution |
RS1027 |  | Types marked with DiagnosticAnalyzerAttribute(s) should inherit from DiagnosticAnalyzer |
RS1028 |  | Provide non-null 'customTags' value to diagnostic descriptor constructor |
RS1029 |  | Do not use reserved diagnostic IDs |
RS1030 |  | Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer |
RS1031 |  | Define diagnostic title correctly |
RS1032 |  | Define diagnostic message correctly |
RS1033 |  | Define diagnostic description correctly |
RS1034 |  | Prefer 'IsKind' for checking syntax kinds |
RS1035 |  | Do not use APIs banned for analyzers |
RS1036 |  | Specify analyzer banned API enforcement setting |
RS1037 |  | Add "CompilationEnd" custom tag to compilation end diagnostic descriptor |
RS1038 | <https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1038.md> | Compiler extensions should be implemented in assemblies with compiler-provided references |
RS1039 |  | This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null' |
RS1040 |  | This call to 'SemanticModel.GetDeclaredSymbol()' will always return 'null' |
RS1041 | <https://github.com/dotnet/roslyn/blob/main/docs/roslyn-analyzers/rules/RS1041.md> | Compiler extensions should be implemented in assemblies targeting netstandard2.0 |
RS1042 |  | Implementations of this interface are not allowed |
RS1043 |  | Do not use file types for implementing analyzers, generators, and code fixers |
RS2000 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Add analyzer diagnostic IDs to analyzer release |
RS2001 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Ensure up-to-date entry for analyzer diagnostic IDs are added to analyzer release |
RS2002 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Do not add removed analyzer diagnostic IDs to unshipped analyzer release |
RS2003 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Shipped diagnostic IDs that are no longer reported should have an entry in the 'Removed Rules' table in unshipped file |
RS2004 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Diagnostic IDs marked as removed in analyzer release file should not be reported by analyzers |
RS2005 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Remove duplicate entries for diagnostic ID in the same analyzer release |
RS2006 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Remove duplicate entries for diagnostic ID between analyzer releases |
RS2007 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Invalid entry in analyzer release file |
RS2008 | <https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md> | Enable analyzer release tracking |
