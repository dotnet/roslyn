// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    using static RoslynDiagnosticsAnalyzersResources;

    /// <summary>
    /// RS0065: <inheritdoc cref="IRemoteJsonServiceParameterTitle"/>
    /// </summary>
#pragma warning disable RS1004 // Recommend adding language support to diagnostic analyzer
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
#pragma warning restore RS1004 // Recommend adding language support to diagnostic analyzer
    public class IRemoteJsonServiceParameterAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.IRemoteJsonServiceParameterRuleId,
            CreateLocalizableResourceString(nameof(IRemoteJsonServiceParameterTitle)),
            CreateLocalizableResourceString(nameof(IRemoteJsonServiceParameterMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(IRemoteJsonServiceParameterDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(context =>
            {
                var remoteJsonService = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisRazorRemoteIRemoteJsonService);
                if (remoteJsonService is null)
                    return;

                var razorSolutionWrapper = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisRazorRemoteRazorSolutionWrapper);
                var documentId = context.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.MicrosoftCodeAnalysisDocumentId);
                if (razorSolutionWrapper is null && documentId is null)
                    return;

                context.RegisterSymbolAction(context => AnalyzeSymbol(context, remoteJsonService, razorSolutionWrapper, documentId), SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(
            SymbolAnalysisContext context,
            INamedTypeSymbol remoteJsonService,
            INamedTypeSymbol? razorPinnedSolutionInfoWrapper,
            INamedTypeSymbol? documentId)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if (namedTypeSymbol.TypeKind != TypeKind.Interface ||
                !namedTypeSymbol.AllInterfaces.Any(i => Equals(i, remoteJsonService)))
            {
                return;
            }

            foreach (var method in namedTypeSymbol.GetMembers().OfType<IMethodSymbol>())
            {
                foreach (var parameter in method.Parameters)
                {
                    if (!Equals(parameter.Type, razorPinnedSolutionInfoWrapper) &&
                        !Equals(parameter.Type, documentId))
                    {
                        continue;
                    }

                    var diagnostic = Diagnostic.Create(
                        Rule,
                        parameter.Locations.FirstOrDefault(),
                        parameter.Name,
                        namedTypeSymbol.Name,
                        method.Name,
                        parameter.Type.Name);
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
