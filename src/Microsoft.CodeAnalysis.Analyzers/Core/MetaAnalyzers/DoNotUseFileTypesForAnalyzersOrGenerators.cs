// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotUseFileTypesForAnalyzersOrGenerators : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.DoNotUseFileTypesForAnalyzersOrGenerators,
            CreateLocalizableResourceString(nameof(DoNotUseFileTypesForAnalyzersOrGeneratorsTitle)),
            CreateLocalizableResourceString(nameof(DoNotUseFileTypesForAnalyzersOrGeneratorsMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotUseFileTypesForAnalyzersOrGeneratorsDescription)));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(context =>
            {
                INamedTypeSymbol? diagnosticAnalyzer = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzer);
                if (diagnosticAnalyzer == null)
                {
                    // Does not contain any diagnostic analyzers.
                    return;
                }

                // There may or may not be a code fix provider type and generator types, depending on what this assembly depends on.
                INamedTypeSymbol? codeFixProvider = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisCodeFixesCodeFixProvider);
                INamedTypeSymbol? isourceGenerator = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisISourceGenerator);
                INamedTypeSymbol? iincrementalGenerator = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisIIncrementalGenerator);

                context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, diagnosticAnalyzer, codeFixProvider, isourceGenerator, iincrementalGenerator), SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol? codeFixProvider, INamedTypeSymbol? isourceGenerator, INamedTypeSymbol? iincrementalGenerator)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

            if (!namedTypeSymbol.IsFileLocal())
            {
                return;
            }

            if (namedTypeSymbol.Inherits(diagnosticAnalyzer) ||
                namedTypeSymbol.Inherits(codeFixProvider) ||
                namedTypeSymbol.Inherits(isourceGenerator) ||
                namedTypeSymbol.Inherits(iincrementalGenerator))
            {
                var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
