// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CSharpAnalyzers
{
    /// <summary>
    /// Analyzer to demonstrate reading an additional file with a structured format.
    /// It looks for an additional file named "Terms.xml" and dumps it to a stream
    /// so that it can be loaded into an <see cref="XDocument"/>. It then extracts
    /// terms from the XML, detects type names that use those terms and reports
    /// diagnostics on them.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    class XmlAdditionalFileAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Type name contains invalid term";
        private const string MessageFormat = "The term '{0}' is not allowed in a type name.";

        private static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.XmlAdditionalFileAnalyzerRuleId,
                Title,
                MessageFormat,
                DiagnosticCategories.AdditionalFile,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                // Find the additional file with the terms.
                ImmutableArray<AdditionalText> additionalFiles = compilationStartContext.Options.AdditionalFiles;
                AdditionalText termsFile = additionalFiles.FirstOrDefault(file => Path.GetFileName(file.Path).Equals("Terms.xml"));

                if (termsFile != null)
                {
                    HashSet<string> terms = new HashSet<string>();
                    SourceText fileText = termsFile.GetText(compilationStartContext.CancellationToken);

                    // Write the additional file back to a stream.
                    MemoryStream stream = new MemoryStream();
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        fileText.Write(writer);
                    }

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
                                symbolAnalysisContext.ReportDiagnostic(
                                    Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], term));
                            }
                        }
                    },
                    SymbolKind.NamedType);
                }
            });
        }
    }
}
