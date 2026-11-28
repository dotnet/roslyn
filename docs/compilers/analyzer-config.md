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

## Global analyzer configs

A config named `.globalconfig`, or containing `is_global = true` before any section header, is a global analyzer config.
Its top-level options apply to all files in the compilation regardless of their location.

Named sections in a global analyzer config identify individual files rather than EditorConfig glob patterns.
A section name can be an absolute path or a path relative to the directory containing the config file.
Relative paths may include `.` and `..` components.
Paths use `/` as the directory separator on all platforms,
and EditorConfig special characters must be escaped with `\` to be treated literally.

For example, in `obj/build.globalconfig`:

```ini
is_global = true

[../Components/Counter.razor]
build_metadata.AdditionalFiles.CustomOption = value
```

The section applies only to `Components/Counter.razor` next to the `obj` directory, not to files with the same name elsewhere.
