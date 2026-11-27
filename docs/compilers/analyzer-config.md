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

## Config-relative build properties

Global analyzer config files can declare config-relative path-valued analyzer options with keys beginning with `config_relative_build_property.`.
The remainder of the key is the name of the `build_property` option to provide, and the value is a path relative to the analyzer config file.
For example:

```ini
is_global = true
config_relative_build_property.ProjectDir = ./
config_relative_build_property.MSBuildProjectDirectory = .
config_relative_build_property.IntermediateOutputPath = obj/Debug/
config_relative_build_property.OutDir = bin/Debug/
config_relative_build_property.ApplicationManifest = obj/app.manifest
```

The compiler resolves the path relative to the containing analyzer config file.
It exposes the named properties as `build_property.<property-name>` options to analyzers and source generators.
A trailing separator in the relative value requests a trailing separator in the resolved value,
which allows the config to describe both directory properties and file properties.
An empty relative value produces an empty `build_property` value.

Explicit `build_property` values override config-relative values.
The `config_relative_build_property` directives themselves are not exposed to analyzers.
