// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    using static CodeAnalysisDiagnosticsResources;

    /// <summary>
    /// RS1027: <inheritdoc cref="ClassIsNotDiagnosticAnalyzerTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ClassIsNotDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.TypeIsNotDiagnosticAnalyzerRuleId,
            CreateLocalizableResourceString(nameof(ClassIsNotDiagnosticAnalyzerTitle)),
            CreateLocalizableResourceString(nameof(ClassIsNotDiagnosticAnalyzerMessage)),
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(csac =>
            {
                var diagnosticAnalyzer = csac.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzer);
                var diagnosticAnalyzerAttribute = csac.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDiagnosticsDiagnosticAnalyzerAttribute);

                if (diagnosticAnalyzer == null || diagnosticAnalyzerAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing Microsoft.CodeAnalysis which defines DiagnosticAnalyzer.
                    return;
                }

                csac.RegisterSymbolAction(sac =>
                {
                    var namedType = (INamedTypeSymbol)sac.Symbol;

                    if (namedType.TypeKind == TypeKind.Class &&
                        namedType.HasAnyAttribute(diagnosticAnalyzerAttribute) &&
                        !namedType.DerivesFrom(diagnosticAnalyzer, baseTypesOnly: true))
                    {
                        sac.ReportDiagnostic(namedType.Locations[0].CreateDiagnostic(Rule, namedType.Name));
                    }
                }, SymbolKind.NamedType);
            });
        }
    }
}
