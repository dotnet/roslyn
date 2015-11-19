// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CSharpAnalyzers
{
    /// <summary>
    /// Analyzer to demonstrate reading an additional file line-by-line.
    /// It looks for an additional file named "Terms.txt" and extracts a set of
    /// terms, one per line. It then detects type names that use those terms and
    /// reports diagnostics.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    class SimpleAdditionalFileAnalyzer : DiagnosticAnalyzer
    {
        private const string Title = "Type name contains invalid term";
        private const string MessageFormat = "The term '{0}' is not allowed in a type name.";

        private static DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(
                DiagnosticIds.SimpleAdditionalFileAnalyzerRuleId,
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
