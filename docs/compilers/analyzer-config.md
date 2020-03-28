Compiler Support for AnalyzerConfig
===================================

AnalyzerConfig is an EditorConfig-superset (https://editorconfig.org/) file
format recognized by the Roslyn command line compiler. The options specified
in analyzer config files are recognized by the compiler in two ways: option
keys following the pattern `dotnet_diagnostic.<diagnostic-id>.severity =
<value>` are parsed and interpreted by the compiler to configure the severity
of compiler diagnostics. `<diagnostic-id>` represents the diagnostic ID
matched by the compiler, case-insensitively, to be configured. `<value>` must
be the name of a member of the
[ReportDiagnostic](../../src/Compilers/Core/Portable/Diagnostic/ReportDiagnostic.cs)
enum, also case-insensitive. These settings are then applied on a
per-SyntaxTree to each of the files whose path matched the AnalyzerConfig
name specification in the compilation.

Any properties which do not have the aforementioned pattern are considered
analyzer options and are placed in a PerTreeOptionsProvider on the
[AnalyzerOptions
type](../../src/Compilers/Core/Portable/DiagnosticAnalyzer/AnalyzerOptions.cs) for
use by analyzers.

AnalyzerConfig files can be passed to the command-line compiler through the
`/analyzerconfig:<file-path>` parameter.