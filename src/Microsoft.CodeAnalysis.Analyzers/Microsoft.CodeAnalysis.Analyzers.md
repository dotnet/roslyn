### RS1001: Missing diagnostic analyzer attribute. ###

Non-abstract sub-types of DiagnosticAnalyzer should be marked with DiagnosticAnalyzerAttribute(s). The argument to this attribute(s), if any, determine the supported languages for the analyzer. Analyzer types without this attribute will be ignored by the analysis engine.

Category: AnalyzerCorrectness

Severity: Warning

### RS1002: Missing kind argument while registering an analyzer action. ###

You must specify at least one syntax/symbol kinds of interest while registering a syntax/symbol analyzer action. Otherwise, the registered action will be dead code and will never be invoked during analysis.

Category: AnalyzerCorrectness

Severity: Warning

### RS1003: Unsupported SymbolKind argument while registering a symbol analyzer action. ###

Category: AnalyzerCorrectness

Severity: Warning

### RS1004: Recommend adding language support to diagnostic analyzer. ###

Diagnostic analyzer is marked as supporting only one language, but the analyzer assembly doesn't seem to refer to any language specific CodeAnalysis assemblies, and so is likely to work for more than one language. Consider adding an additional language argument to DiagnosticAnalyzerAttribute.

Category: AnalyzerCorrectness

Severity: Warning

### RS1005: ReportDiagnostic invoked with an unsupported DiagnosticDescriptor. ###

ReportDiagnostic should only be invoked with supported DiagnosticDescriptors that are returned from DiagnosticAnalyzer.SupportedDiagnostics property. Otherwise, the reported diagnostic will be filtered out by the analysis engine.

Category: AnalyzerCorrectness

Severity: Warning

### RS1006: Invalid type argument for DiagnosticAnalyzer's Register method. ###

DiagnosticAnalyzer's language-specific Register methods, such as RegisterSyntaxNodeAction, RegisterCodeBlockStartAction and RegisterCodeBlockEndAction, expect a language-specific 'SyntaxKind' type argument for it's 'TLanguageKindEnumName' type parameter. Otherwise, the registered analyzer action can never be invoked during analysis.

Category: AnalyzerCorrectness

Severity: Warning

### RS1007: Provide localizable arguments to diagnostic descriptor constructor. ###

If your diagnostic analyzer and it's reported diagnostics need to be localizable, then the supported DiagnosticDescriptors used for constructing the diagnostics must also be localizable. If so, then localizable argument(s) must be provided for parameter 'title' (and optionally 'description') to the diagnostic descriptor constructor to ensure that the descriptor is localizable.

Category: AnalyzerLocalization

Severity: Warning

### RS1008: Avoid storing per-compilation data into the fields of a diagnostic analyzer. ###

Instance of a diagnostic analyzer might outlive the lifetime of compilation. Hence, storing per-compilation data, such as symbols, into the fields of a diagnostic analyzer might cause stale compilations to stay alive and cause memory leaks.  Instead, you should store this data on a separate type instantiated in a compilation start action, registered using 'AnalysisContext.RegisterCompilationStartAction' API. An instance of this type will be created per-compilation and it won't outlive compilation's lifetime, hence avoiding memory leaks.

Category: AnalyzerPerformance

Severity: Warning

### RS1009: Only internal implementations of this interface are allowed. ###

The author of this interface did not intend to have third party implementations of this interface and reserves the right to change it. Implementing this interface could therefore result in a source or binary compatibility issue with a future version of this interface.

Category: Compatibility

Severity: Error

### RS1010: Create code actions should have a unique EquivalenceKey for FixAll occurrences support. ###

A CodeFixProvider that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer. This enables the FixAllProvider to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.

Category: Correctness

Severity: Warning

### RS1011: Use code actions that have a unique EquivalenceKey for FixAll occurrences support. ###

A CodeFixProvider that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique across all registered code actions by this fixer. This enables the FixAllProvider to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.

Category: Correctness

Severity: Warning

### RS1012: Start action has no registered actions. ###

An analyzer start action enables performing stateful analysis over a given code unit, such as a code block, compilation, etc. Careful design is necessary to achieve efficient analyzer execution without memory leaks. Use the following guidelines for writing such analyzers:
1. Define a new scope for the registered start action, possibly with a private nested type for analyzing each code unit.
2. If required, define and initialize state in the start action.
3. Register at least one non-end action that refers to this state in the start action. If no such action is necessary, consider replacing the start action with a non-start action. For example, a CodeBlockStartAction with no registered actions or only a registered CodeBlockEndAction should be replaced with a CodeBlockAction.
4. If required, register an end action to report diagnostics based on the final state.


Category: AnalyzerPerformance

Severity: Warning

### RS1013: Start action has no registered non-end actions. ###

An analyzer start action enables performing stateful analysis over a given code unit, such as a code block, compilation, etc. Careful design is necessary to achieve efficient analyzer execution without memory leaks. Use the following guidelines for writing such analyzers:
1. Define a new scope for the registered start action, possibly with a private nested type for analyzing each code unit.
2. If required, define and initialize state in the start action.
3. Register at least one non-end action that refers to this state in the start action. If no such action is necessary, consider replacing the start action with a non-start action. For example, a CodeBlockStartAction with no registered actions or only a registered CodeBlockEndAction should be replaced with a CodeBlockAction.
4. If required, register an end action to report diagnostics based on the final state.


Category: AnalyzerPerformance

Severity: Warning