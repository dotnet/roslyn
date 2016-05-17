Introduction
============
The C# and Visual Basic compilers support a /errorlog:<file> switch on
the command line to log all diagnostics in a structured, JSON format.

The log format is SARIF (Static Analysis Results Interchange Format)
and is defined by https://github.com/sarif-standard/sarif-spec

Note that the format has not been finalized and the specification is
still a draft. It will remain subject to breaking changes until the
`version` property is emitted with a value of "1.0" or greater.

This document does not repeat the details of the SARIF format, but
rather adds information that is specific to the implementation provided
by the C# and Visual Basic Compilers.


Result Properties
================
The SARIF standard allows the `properties` property of `result` objects
to contain arbitrary (string, string) key-value pairs.

The keys and values used by the C# and VB compilers are serialized from
the corresponding `Microsoft.CodeAnalysis.Diagnostic` as follows:

Key                      | Value
------------------------ | ------------
"warningLevel"           | `Diagnostic.WarningLevel`
"category"               | `Diagnostic.Category`
"isEnabledByDefault"     | `Diagnostic.IsEnabledByDefault
"customProperties"       | `Diagnostic.Properties` 
