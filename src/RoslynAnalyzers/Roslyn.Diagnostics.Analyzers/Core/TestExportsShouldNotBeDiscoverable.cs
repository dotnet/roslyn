// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

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
    using static RoslynDiagnosticsAnalyzersResources;

    /// <summary>
    /// RS0032: <inheritdoc cref="TestExportsShouldNotBeDiscoverableTitle"/>
    /// MEF-exported types defined in test assemblies should be marked with <see cref="PartNotDiscoverableAttribute"/>
    /// to avoid polluting the container(s) created for testing. These parts should be explicitly added to the container
    /// when required for specific tests.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class TestExportsShouldNotBeDiscoverable : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.TestExportsShouldNotBeDiscoverableRuleId,
            CreateLocalizableResourceString(nameof(TestExportsShouldNotBeDiscoverableTitle)),
            CreateLocalizableResourceString(nameof(TestExportsShouldNotBeDiscoverableMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: CreateLocalizableResourceString(nameof(TestExportsShouldNotBeDiscoverableDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

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

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol? exportAttribute, INamedTypeSymbol namedType, IEnumerable<AttributeData> exportAttributes, IEnumerable<AttributeData> namedTypeAttributes)
        {
            if (exportAttribute is null)
            {
                return;
            }

            var exportAttributeApplication = exportAttributes.FirstOrDefault(ad => ad.AttributeClass.DerivesFrom(exportAttribute));
            if (exportAttributeApplication is null)
            {
                return;
            }

            if (!namedTypeAttributes.Any(ad =>
                ad.AttributeClass.Name == nameof(PartNotDiscoverableAttribute)
                && Equals(ad.AttributeClass.ContainingNamespace, exportAttribute.ContainingNamespace)))
            {
                if (exportAttributeApplication.ApplicationSyntaxReference == null)
                {
                    context.ReportDiagnostic(context.Symbol.CreateDiagnostic(Rule, namedType.Name));
                }
                else
                {
                    // '{0}' is exported for test purposes and should be marked PartNotDiscoverable
                    context.ReportDiagnostic(exportAttributeApplication.ApplicationSyntaxReference.CreateDiagnostic(Rule, context.CancellationToken, namedType.Name));
                }
            }
        }
    }
}
