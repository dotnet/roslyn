// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ClassIsNotDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitleNotDiagnosticAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ClassIsNotDiagnosticAnalyzerTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static readonly LocalizableString s_localizableMessageNotDiagnosticAnalyzer = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.ClassIsNotDiagnosticAnalyzerMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticIds.TypeIsNotDiagnosticAnalyzerRuleId,
            s_localizableTitleNotDiagnosticAnalyzer,
            s_localizableMessageNotDiagnosticAnalyzer,
            DiagnosticCategory.MicrosoftCodeAnalysisCorrectness,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
                        namedType.HasAttribute(diagnosticAnalyzerAttribute) &&
                        !namedType.DerivesFrom(diagnosticAnalyzer, baseTypesOnly: true))
                    {
                        sac.ReportDiagnostic(namedType.Locations[0].CreateDiagnostic(Rule, namedType.Name));
                    }
                }, SymbolKind.NamedType);
            });
        }
    }
}
