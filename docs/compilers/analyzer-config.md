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

Path Matching and Case Sensitivity
-----------------------------------

The compiler matches source file paths against AnalyzerConfig directories
using **case-sensitive (ordinal) string comparison**, even on Windows. This is
a deliberate design decision: on Windows, individual directories can be
configured as case-sensitive (e.g. via
[WSL case sensitivity](https://learn.microsoft.com/en-us/windows/wsl/case-sensitivity)),
and it is possible for a single path to traverse both case-sensitive and
case-insensitive segments. Since there is no reliable way to determine the
case-sensitivity of each segment without querying the file system per
directory, the compiler uses ordinal comparison to give a consistent answer
regardless of the environment.

The one exception is the drive letter on Windows: `c:\src` and `C:\src` are
normalized to the same uppercase form (`C:\src`) before comparison, since
Windows treats drive letters as case-insensitive in all configurations.

In practice this means that the casing of the directory containing the
`.editorconfig` file must exactly match the casing of the source file paths
passed to the compiler for the configuration to apply (aside from the drive
letter).
