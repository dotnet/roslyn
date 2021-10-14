# Microsoft.CodeAnalysis.Analyzers

## RS1001: Missing diagnostic analyzer attribute

Non-abstract sub-types of DiagnosticAnalyzer should be marked with DiagnosticAnalyzerAttribute(s). The argument to this attribute(s), if any, determine the supported languages for the analyzer. Analyzer types without this attribute will be ignored by the analysis engine.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1002: Missing kind argument when registering an analyzer action

You must specify at least one syntax, symbol or operation kind when registering a syntax, symbol, or operation analyzer action respectively. Otherwise, the registered action will never be invoked during analysis.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1003: Unsupported SymbolKind argument when registering a symbol analyzer action

SymbolKind '{0}' is not supported for symbol analyzer actions

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1004: Recommend adding language support to diagnostic analyzer

Diagnostic analyzer is marked as supporting only one language, but the analyzer assembly doesn't seem to refer to any language specific CodeAnalysis assemblies, and so is likely to work for more than one language. Consider adding an additional language argument to DiagnosticAnalyzerAttribute.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1005: ReportDiagnostic invoked with an unsupported DiagnosticDescriptor

ReportDiagnostic should only be invoked with supported DiagnosticDescriptors that are returned from DiagnosticAnalyzer.SupportedDiagnostics property. Otherwise, the reported diagnostic will be filtered out by the analysis engine.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1006: Invalid type argument for DiagnosticAnalyzer's Register method

DiagnosticAnalyzer's language-specific Register methods, such as RegisterSyntaxNodeAction, RegisterCodeBlockStartAction and RegisterCodeBlockEndAction, expect a language-specific 'SyntaxKind' type argument for it's 'TLanguageKindEnumName' type parameter. Otherwise, the registered analyzer action can never be invoked during analysis.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1007: Provide localizable arguments to diagnostic descriptor constructor

If your diagnostic analyzer and it's reported diagnostics need to be localizable, then the supported DiagnosticDescriptors used for constructing the diagnostics must also be localizable. If so, then localizable argument(s) must be provided for parameter 'title' (and optionally 'description') to the diagnostic descriptor constructor to ensure that the descriptor is localizable.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisLocalization|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## RS1008: Avoid storing per-compilation data into the fields of a diagnostic analyzer

Instance of a diagnostic analyzer might outlive the lifetime of compilation. Hence, storing per-compilation data, such as symbols, into the fields of a diagnostic analyzer might cause stale compilations to stay alive and cause memory leaks.  Instead, you should store this data on a separate type instantiated in a compilation start action, registered using 'AnalysisContext.RegisterCompilationStartAction' API. An instance of this type will be created per-compilation and it won't outlive compilation's lifetime, hence avoiding memory leaks.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1009: Only internal implementations of this interface are allowed

The author of this interface did not intend to have third party implementations of this interface and reserves the right to change it. Implementing this interface could therefore result in a source or binary compatibility issue with a future version of this interface.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCompatibility|
|Enabled|True|
|Severity|Error|
|CodeFix|False|
---

## RS1010: Create code actions should have a unique EquivalenceKey for FixAll occurrences support

A CodeFixProvider that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique for each kind of code action created by this fixer. This enables the FixAllProvider to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.

|Item|Value|
|-|-|
|Category|Correctness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1011: Use code actions that have a unique EquivalenceKey for FixAll occurrences support

A CodeFixProvider that intends to support fix all occurrences must classify the registered code actions into equivalence classes by assigning it an explicit, non-null equivalence key which is unique for each kind of code action created by this fixer. This enables the FixAllProvider to fix all diagnostics in the required scope by applying code actions from this fixer that are in the equivalence class of the trigger code action.

|Item|Value|
|-|-|
|Category|Correctness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1012: Start action has no registered actions

An analyzer start action enables performing stateful analysis over a given code unit, such as a code block, compilation, etc. Careful design is necessary to achieve efficient analyzer execution without memory leaks. Use the following guidelines for writing such analyzers:

1. Define a new scope for the registered start action, possibly with a private nested type for analyzing each code unit.

2. If required, define and initialize state in the start action.

3. Register at least one non-end action that refers to this state in the start action. If no such action is necessary, consider replacing the start action with a non-start action. For example, a CodeBlockStartAction with no registered actions or only a registered CodeBlockEndAction should be replaced with a CodeBlockAction.

4. If required, register an end action to report diagnostics based on the final state.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1013: Start action has no registered non-end actions

An analyzer start action enables performing stateful analysis over a given code unit, such as a code block, compilation, etc. Careful design is necessary to achieve efficient analyzer execution without memory leaks. Use the following guidelines for writing such analyzers:

1. Define a new scope for the registered start action, possibly with a private nested type for analyzing each code unit.

2. If required, define and initialize state in the start action.

3. Register at least one non-end action that refers to this state in the start action. If no such action is necessary, consider replacing the start action with a non-start action. For example, a CodeBlockStartAction with no registered actions or only a registered CodeBlockEndAction should be replaced with a CodeBlockAction.

4. If required, register an end action to report diagnostics based on the final state.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1014: Do not ignore values returned by methods on immutable objects.

Many objects exposed by Roslyn are immutable. The return value from a method invocation on these objects should not be ignored.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1015: Provide non-null 'helpLinkUri' value to diagnostic descriptor constructor

The 'helpLinkUri' value is used to show information when this diagnostic in the error list. Every analyzer should have a helpLinkUri specified which points to a help page that does not change over time.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDocumentation|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## RS1016: Code fix providers should provide FixAll support

A CodeFixProvider should provide FixAll support to enable users to fix multiple instances of the underlying diagnostic with a single code fix. See documentation at <https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md> for further details.

|Item|Value|
|-|-|
|Category|Correctness|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1017: DiagnosticId for analyzers must be a non-null constant

DiagnosticId for analyzers must be a non-null constant.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1018: DiagnosticId for analyzers must be in specified format

DiagnosticId for analyzers must be in specified format.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1019: DiagnosticId must be unique across analyzers

DiagnosticId must be unique across analyzers.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1020: Category for analyzers must be from the specified values

Category for analyzers must be from the specified values.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## RS1021: Invalid entry in analyzer category and diagnostic ID range specification file

Invalid entry in analyzer category and diagnostic ID range specification file.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1022: Do not use types from Workspaces assembly in an analyzer

Diagnostic analyzer types should not use types from Workspaces assemblies. Workspaces assemblies are only available when the analyzer executes in Visual Studio IDE live analysis, but are not available during command line build. Referencing types from Workspaces assemblies will lead to runtime exception during analyzer execution in command line build.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS1023](https://go.microsoft.com/fwlink/?linkid=874285): Upgrade MSBuildWorkspace

MSBuildWorkspace has moved to the Microsoft.CodeAnalysis.Workspaces.MSBuild NuGet package and there are breaking API changes.

|Item|Value|
|-|-|
|Category|Library|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1024: Symbols should be compared for equality

Symbols should be compared for equality, not identity. Use an overload accepting an 'IEqualityComparer' and pass 'SymbolEqualityComparer'.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1025: Configure generated code analysis

Configure generated code analysis

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1026: Enable concurrent execution

Enable concurrent execution

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1027: Types marked with DiagnosticAnalyzerAttribute(s) should inherit from DiagnosticAnalyzer

Inherit type '{0}' from DiagnosticAnalyzer or remove the DiagnosticAnalyzerAttribute(s)

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1028: Provide non-null 'customTags' value to diagnostic descriptor constructor

The 'customTags' value is used as a way to enable specific actions and filters on diagnostic descriptors based on the specific values of the tags. Every Roslyn analyzer should have at least one tag from the 'WellKnownDiagnosticTags' class.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDocumentation|
|Enabled|False|
|Severity|Warning|
|CodeFix|False|
---

## RS1029: Do not use reserved diagnostic IDs

DiagnosticId for analyzers should not use reserved IDs.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1030: Do not invoke Compilation.GetSemanticModel() method within a diagnostic analyzer

'GetSemanticModel' is an expensive method to invoke within a diagnostic analyzer because it creates a completely new semantic model, which does not share compilation data with the compiler or other analyzers. This incurs an additional performance cost during semantic analysis. Instead, consider registering a different analyzer action which allows used of a shared 'SemanticModel', such as 'RegisterOperationAction', 'RegisterSyntaxNodeAction', or 'RegisterSemanticModelAction'.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisCorrectness|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## RS1031: Define diagnostic title correctly

The diagnostic title should not contain a period, nor any line return character, nor any leading or trailing whitespaces

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1032: Define diagnostic message correctly

The diagnostic message should not contain any line return character nor any leading or trailing whitespaces and should either be a single sentence without a trailing period or a multi-sentences with a trailing period

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1033: Define diagnostic description correctly

The diagnostic description should be one or multiple sentences ending with a punctuation sign and should not have any leading or trailing whitespaces

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisDesign|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## RS1034: Prefer 'IsKind' for checking syntax kinds

Prefer 'syntax.IsKind(kind)' to 'syntax.Kind() == kind' when checking syntax kinds. Code using 'IsKind' is slightly more efficient at runtime, so consistent use of this form where applicable helps improve performance in complex analysis scenarios.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisPerformance|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## [RS2000](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Add analyzer diagnostic IDs to analyzer release

All supported analyzer diagnostic IDs should be part of an analyzer release.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## [RS2001](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Ensure up-to-date entry for analyzer diagnostic IDs are added to analyzer release

Ensure up-to-date entry for analyzer diagnostic IDs are added to analyzer release.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---

## [RS2002](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Do not add removed analyzer diagnostic IDs to unshipped analyzer release

Entries for analyzer diagnostic IDs that are no longer reported and never shipped can be removed from unshipped analyzer release.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS2003](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Shipped diagnostic IDs that are no longer reported should have an entry in the 'Removed Rules' table in unshipped file

Shipped diagnostic IDs that are no longer reported should have an entry in the 'Removed Rules' table in unshipped file.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS2004](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Diagnostic IDs marked as removed in analyzer release file should not be reported by analyzers

Diagnostic IDs marked as removed in analyzer release file should not be reported by analyzers.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS2005](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Remove duplicate entries for diagnostic ID in the same analyzer release

Remove duplicate entries for diagnostic ID in the same analyzer release.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS2006](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Remove duplicate entries for diagnostic ID between analyzer releases

Remove duplicate entries for diagnostic ID between analyzer releases.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS2007](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Invalid entry in analyzer release file

Invalid entry in analyzer release file.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|False|
---

## [RS2008](https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md): Enable analyzer release tracking

Enabling release tracking for analyzer packages helps in tracking and documenting the analyzer diagnostics that ship and/or change with each analyzer release. See details at <https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md>.

|Item|Value|
|-|-|
|Category|MicrosoftCodeAnalysisReleaseTracking|
|Enabled|True|
|Severity|Warning|
|CodeFix|True|
---
