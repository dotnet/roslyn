// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class CreateTestAccessor : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.CreateTestAccessorTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.CreateTestAccessorMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.CreateTestAccessorDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static readonly DiagnosticDescriptor CreateTestAccessorRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.CreateTestAccessorRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslynDiagnosticsMaintainability,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: s_localizableDescription,
            customTags: new[] { WellKnownDiagnosticTags.Telemetry, WellKnownDiagnosticTags.NotConfigurable });

        public static string GetTestAccessorMethodName => "GetTestAccessor";
        public static string TestAccessorTypeName => "TestAccessor";

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CreateTestAccessorRule);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterSymbolAction(HandleNamedType, SymbolKind.NamedType);
        }

        private static void HandleNamedType(SymbolAnalysisContext context)
        {
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.Name == TestAccessorTypeName)
            {
                return;
            }

            if (symbol.Locations.IsEmpty || !symbol.Locations[0].IsInSource)
            {
                return;
            }

            if (symbol.GetTypeMembers(TestAccessorTypeName).Any())
            {
                return;
            }

            context.ReportDiagnostic(symbol.CreateDiagnostic(CreateTestAccessorRule));
        }
    }
}
