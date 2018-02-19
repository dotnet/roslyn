Introduction
============

This document covers the following:

* Uses of Additional Files
* Passing Additional Files on the command line
* Specifying an individual item as an `AdditionalFile` in an MSBuild project
* Specifying an entire set of items as `AdditionalFile`s in an MSBuild project
* Accessing and reading additional files through the `AnalyzerOptions` type
* Includes sample analyzers that read additional files

Uses
====

Sometimes an analyzer needs access to information that is not available through normal compiler inputs--source files, references, and options. To support these scenarios the C# and Visual Basic compilers can accept additional, non-source text files as inputs.

For example, an analyzer may enforce that a set of banned terms is not used within a project, or that every source file has a certain copyright header. The terms or copyright header could be passed to the analyzer as an additional file, rather than being hard-coded in the analyzer itself.

On the Command Line
===================

On the command line, additional files can be passed using the `/additionalfile` option. For example:
```
csc.exe alpha.cs /additionalfile:terms.txt
```

In a Project File
=================

Passing an Individual File
--------------------------

To specify an individual project item as an additional file, set the item type to `AdditionalFiles`:

``` XML
<ItemGroup>
  <AdditionalFiles Include="terms.txt" />
</ItemGroup>
```

Passing a Group of Files
------------------------

Sometimes it isn't possible to change the item type, or a whole set of items need to be passed as additional files. In this situation you can update the `AdditionalFileItemNames` property to specify which item types to include. For example, if your analyzer needs access to all .resx files in the project, you can do the following:
``` XML
<PropertyGroup>
  <!-- Update the property to include all EmbeddedResource files -->
  <AdditionalFileItemNames>$(AdditionalFileItemNames);EmbeddedResource</AdditionalFileItemNames>
</PropertyGroup>
<ItemGroup>
  <!-- Existing resource file -->
  <EmbeddedResource Include="Terms.resx">
    ...
  </EmbeddedResource>
</ItemGroup>
```

Accessing Additional Files
==========================

The set of additional files can be accessed via the `Options` property of the context object passed to a diagnostic action. For example:
```C#
CompilationAnalysisContext context = ...;
ImmutableArray<AdditionalText> additionFiles = context.Options.AdditionalFiles;
```

From an `AdditionalText` instance, you can access the path to the file or the contents as a `SourceText`:
``` C#
AdditionText additionalFile = ...;
string path = additionalFile.Path;
SourceText contents = additionalFile.GetText();
```

Samples
=======

Reading a File Line-by-Line
---------------------------

This sample reads a simple text file for a set of terms--one per line--that should not be used in type names.

``` C#
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public class CheckTermsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CheckTerms001";

    private const string Title = "Type name contains invalid term";
    private const string MessageFormat = "The term '{0}' is not allowed in a type name.";
    private const string Category = "Policy";

    private static DiagnosticDescriptor Rule =
        new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Find the file with the invalid terms.
            ImmutableArray<AdditionalText> additionalFiles = compilationStartContext.Options.AdditionalFiles;
            AdditionalText termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("Terms.txt"));

            if (termsFile != null)
            {
                HashSet<string> terms = new HashSet<string>();

                // Read the file line-by-line to get the terms.
                SourceText fileText = termsFile.GetText(compilationStartContext.CancellationToken);
                foreach (TextLine line in fileText.Lines)
                {
                    terms.Add(line.ToString());
                }

                // Check every named type for the invalid terms.
                compilationStartContext.RegisterSymbolAction(symbolAnalysisContext =>
                {
                    INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)symbolAnalysisContext.Symbol;
                    string symbolName = namedTypeSymbol.Name;

                    foreach (string term in terms)
                    {
                        if (symbolName.Contains(term))
                        {
                            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], term));
                        }
                    }
                },
                SymbolKind.NamedType);
            }
        });
    }
}

```

Converting a File to a Stream
-----------------------------

In cases where an additional file contains structured data (e.g., XML or JSON) the line-by-line access provided by the `SourceText` may not be desirable. One alternative is to convert a `SourceText` to a `string`, by calling `ToString()` on it. This sample demonstrates another alternative: converting a `SourceText` to a `Stream` for consumption by other libraries. The terms file is assumed to have the following format:

``` XML
<Terms>
  <Term>frob</Term>
  <Term>wizbang</Term>
  <Term>orange</Term>
</Terms>
```

``` C#
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

[DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
public class CheckTermsXMLAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "CheckTerms001";

    private const string Title = "Type name contains invalid term";
    private const string MessageFormat = "The term '{0}' is not allowed in a type name.";
    private const string Category = "Policy";

    private static DiagnosticDescriptor Rule =
        new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            // Find the file with the invalid terms.
            ImmutableArray<AdditionalText> additionalFiles = compilationStartContext.Options.AdditionalFiles;
            AdditionalText termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("Terms.xml"));

            if (termsFile != null)
            {
                HashSet<string> terms = new HashSet<string>();
                SourceText fileText = termsFile.GetText(compilationStartContext.CancellationToken);

                MemoryStream stream = new MemoryStream();
                using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
                {
                    fileText.Write(writer);
                }
                
                stream.Position = 0;

                // Read all the <Term> elements to get the terms.
                XDocument document = XDocument.Load(stream);
                foreach (XElement termElement in document.Descendants("Term"))
                {
                    terms.Add(termElement.Value);
                }

                // Check every named type for the invalid terms.
                compilationStartContext.RegisterSymbolAction(symbolAnalysisContext =>
                {
                    INamedTypeSymbol namedTypeSymbol = (INamedTypeSymbol)symbolAnalysisContext.Symbol;
                    string symbolName = namedTypeSymbol.Name;

                    foreach (string term in terms)
                    {
                        if (symbolName.Contains(term))
                        {
                            symbolAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], term));
                        }
                    }
                },
                SymbolKind.NamedType);
            }
        });
    }
}
```
