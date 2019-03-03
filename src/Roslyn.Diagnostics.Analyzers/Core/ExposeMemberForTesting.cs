// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class ExposeMemberForTesting : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ExposeMemberForTestingTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ExposeMemberForTestingMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ExposeMemberForTestingDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static readonly DiagnosticDescriptor ExposeMemberForTestingRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.ExposeMemberForTestingRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: new[] { WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable });

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ExposeMemberForTestingRule);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(HandleNamedType, SymbolKind.NamedType);
        }

        private static void HandleNamedType(SymbolAnalysisContext context)
        {
            if (context.Symbol.Name != CreateTestAccessor.TestAccessorTypeName)
            {
                return;
            }

            if (context.Symbol.Locations.IsEmpty || !context.Symbol.Locations[0].IsInSource)
            {
                return;
            }

            if (!(context.Symbol.ContainingSymbol is INamedTypeSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(ExposeMemberForTestingRule, context.Symbol.Locations[0]));
        }
    }
}
