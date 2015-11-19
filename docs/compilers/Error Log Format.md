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


Issue Properties
================
The SARIF standard allows the `properties` property of `issue` objects
to contain arbitrary (string, string) key-value pairs.

The keys and values used by the C# and VB compilers are serialized from
the corresponding `Microsoft.CodeAnalysis.Diagnostic` and its
`Microsoft.CodeAnalysis.DiagnosticDescriptor` as follows:

Key                      | Value
------------------------ | ------------
"severity"               | `Diagnostic.Severity` ("Hidden", "Info", "Warning, or "Error")
"warningLevel"           | `Diagnostic.WarningLevel` ("1", "2", "3", or "4" for "Warning" severity; omitted otherwise)
"defaultSeverity"        | `Diagnostic.DefaultSeverity` ("Hidden", "Info", "Warning, "Error")
"title"                  | `DiagnosticDesciptor.Title` (omitted if null or empty)
"category"               | `Diagnostic.Category`
"helpLink"               | `DiagnosticDescriptor.HelpLink` (omitted if null or empty)
"isEnabledByDefault"     | `Diagnostic.IsEnabledByDefault` ("True" or "False")
"isSuppressedInSource"   | `Diagnostic.IsSuppressedInSource` ("True" or "False")
"customTags"             | `Diagnostic.CustomTags` (joined together in a `;`-delimted list)
"customProperties.[key]" | `Diagnostic.Properties[key]` (for each key in the dictionary)
