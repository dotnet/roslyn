// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    /// <summary>
    /// MEF-exported types defined in test assemblies should be marked with <see cref="PartNotDiscoverableAttribute"/>
    /// to avoid polluting the container(s) created for testing. These parts should be explicitly added to the container
    /// when required for specific tests.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class TestExportsShouldNotBeDiscoverable : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.TestExportsShouldNotBeDiscoverableTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.TestExportsShouldNotBeDiscoverableMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.TestExportsShouldNotBeDiscoverableDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.TestExportsShouldNotBeDiscoverableRuleId,
            s_localizableTitle,
            s_localizableMessage,
            DiagnosticCategory.RoslyDiagnosticsReliability,
            DiagnosticHelpers.DefaultDiagnosticSeverity,
            isEnabledByDefault: false,
            description: s_localizableDescription,
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var exportAttributeV1 = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionExportAttribute);
                var exportAttributeV2 = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCompositionExportAttribute);
                var inheritedExportAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionInheritedExportAttribute);
                var attributeUsageAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemAttributeUsageAttribute);

                if (exportAttributeV1 is null && exportAttributeV2 is null)
                {
                    // We don't need to check assemblies unless they're referencing MEF, so we're done
                    return;
                }

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    var exportAttributes = namedType.GetApplicableExportAttributes(exportAttributeV1, exportAttributeV2, inheritedExportAttribute);
                    var namedTypeAttributes = namedType.GetApplicableAttributes(attributeUsageAttribute);

                    AnalyzeSymbolForAttribute(ref symbolContext, exportAttributeV1, namedType, exportAttributes, namedTypeAttributes);
                    AnalyzeSymbolForAttribute(ref symbolContext, exportAttributeV2, namedType, exportAttributes, namedTypeAttributes);
                }, SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol? exportAttributeOpt, INamedTypeSymbol namedType, IEnumerable<AttributeData> exportAttributes, IEnumerable<AttributeData> namedTypeAttributes)
        {
            if (exportAttributeOpt is null)
            {
                return;
            }

            var exportAttributeApplication = exportAttributes.FirstOrDefault(ad => ad.AttributeClass.DerivesFrom(exportAttributeOpt));
            if (exportAttributeApplication is null)
            {
                return;
            }

            if (!namedTypeAttributes.Any(ad =>
                ad.AttributeClass.Name == nameof(PartNotDiscoverableAttribute)
                && Equals(ad.AttributeClass.ContainingNamespace, exportAttributeOpt.ContainingNamespace)))
            {
                // '{0}' is exported for test purposes and should be marked PartNotDiscoverable
                context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
            }
        }
    }
}
