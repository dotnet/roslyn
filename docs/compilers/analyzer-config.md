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

Path mapping and global configs
-------------------------------

A global analyzer config (`is_global = true`) identifies the file each section
applies to by its full path in the section header. To keep such a config
independent of the directory a build was run in, the compiler can apply the
`/pathmap` substitutions to a source file's path before matching it against a
global config's section headers (a file's real path is still tried as well, so
configs written with unmapped paths always continue to match). This matching is
additive and always safe, so it is unconditional in both the command-line
compiler and the workspace/IDE: `AnalyzerConfigSet.Create` accepts the path map,
the command line passes `Arguments.PathMap`, and the workspace derives it from
the project's `CompilationOptions.SourceReferenceResolver`.

Writing the mapped paths into the generated config is opt-in, because it changes
the bytes a build produces and can hand a source generator a non-openable mapped
path. The MSBuild `GenerateMSBuildEditorConfig` task, which produces the global
config from `CompilerVisibleProperty` / `CompilerVisibleItemMetadata`, exposes
two independent switches (both **off by default**):

- `MapGeneratedMSBuildEditorConfigPaths` maps the section-header file paths.
- `MapGeneratedMSBuildEditorConfigPropertyValues` maps path-valued property
  values, i.e. any `build_property.<Name>` value that begins with a mapped root
  (for example `build_property.ProjectDir`). Values that are not paths, or paths
  outside every mapped root, are always left unchanged (the map is
  prefix-anchored).

Enabling both makes the generated config byte-for-byte identical across checkout
roots, which is what deterministic and cacheable builds need. They are separate
switches because mapping section headers is safe (the compiler tries the real
path too), whereas mapping a property value can break a source generator that
reads that value and opens or embeds it. Because the switches default off, a
project that does not set them is completely unaffected. Directory-scoped
(non-global) editorconfig matching is never affected by `/pathmap`; it always
uses the real on-disk path.