// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

using System;
using System.Collections.Generic;
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
    /// RS0006: <inheritdoc cref="DoNotMixAttributesFromDifferentVersionsOfMEFTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotMixAttributesFromDifferentVersionsOfMEFAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.MixedVersionsOfMefAttributesRuleId,
            CreateLocalizableResourceString(nameof(DoNotMixAttributesFromDifferentVersionsOfMEFTitle)),
            CreateLocalizableResourceString(nameof(DoNotMixAttributesFromDifferentVersionsOfMEFMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(DoNotMixAttributesFromDifferentVersionsOfMEFDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var mefV1ExportAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionExportAttribute);
                var mefV2ExportAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCompositionExportAttribute);
                if (mefV1ExportAttribute == null || mefV2ExportAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing both versions of MEF, so we're done
                    return;
                }

                var attributeUsageAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemAttributeUsageAttribute);

                var exportAttributes = new List<INamedTypeSymbol>() { mefV1ExportAttribute, mefV2ExportAttribute };
                compilationContext.RegisterSymbolAction(c => AnalyzeSymbol(c, exportAttributes, attributeUsageAttribute), SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext symbolContext, IEnumerable<INamedTypeSymbol> exportAttributes, INamedTypeSymbol? attributeUsageAttribute)
        {
            var namedType = (INamedTypeSymbol)symbolContext.Symbol;
            var namedTypeAttributes = namedType.GetApplicableAttributes(attributeUsageAttribute);

            // Figure out which export attributes are being used here
            var appliedExportAttributes = exportAttributes.Where(e => namedTypeAttributes.Any(ad => ad.AttributeClass.DerivesFrom(e))).ToList();

            // If we have no exports we're done
            if (appliedExportAttributes.Count == 0)
            {
                return;
            }

            var badNamespaces = exportAttributes.Except(appliedExportAttributes).Select(s => s.ContainingNamespace).ToSet();
            var goodNamespaces = appliedExportAttributes.Select(s => s.ContainingNamespace).ToSet();

            // Now look at all attributes and see if any are metadata attributes from badNamespaces, but none from good namepaces.
            foreach (var namedTypeAttribute in namedTypeAttributes)
            {
                var appliedMetadataAttributes = namedTypeAttribute.AttributeClass.GetApplicableAttributes(attributeUsageAttribute)
                    .Where(ad => ad.AttributeClass.Name.Equals("MetadataAttributeAttribute", StringComparison.Ordinal));
                if (appliedMetadataAttributes.Any(ad => badNamespaces.Contains(ad.AttributeClass.ContainingNamespace)) &&
                    !appliedMetadataAttributes.Any(ad => goodNamespaces.Contains(ad.AttributeClass.ContainingNamespace)))
                {
                    ReportDiagnostic(symbolContext, namedType, namedTypeAttribute);
                }
            }

            // Also look through all members and their attributes, and see if any are using from bad places
            foreach (var member in namedType.GetMembers())
            {
                foreach (var attribute in member.GetAttributes())
                {
                    if (badNamespaces.Contains(attribute.AttributeClass.ContainingNamespace))
                    {
                        ReportDiagnostic(symbolContext, namedType, attribute);
                    }
                }

                // if it's a constructor, we should also check parameters since they may have [ImportMany]

                if (member is IMethodSymbol methodSymbol && methodSymbol.MethodKind == MethodKind.Constructor)
                {
                    foreach (var parameter in methodSymbol.Parameters)
                    {
                        foreach (var attribute in parameter.GetAttributes())
                        {
                            if (badNamespaces.Contains(attribute.AttributeClass.ContainingNamespace))
                            {
                                ReportDiagnostic(symbolContext, namedType, attribute);
                            }
                        }
                    }
                }
            }
        }

        private static void ReportDiagnostic(SymbolAnalysisContext symbolContext, INamedTypeSymbol exportedType, AttributeData problematicAttribute)
        {
            if (problematicAttribute.ApplicationSyntaxReference == null)
            {
                symbolContext.ReportDiagnostic(symbolContext.Symbol.CreateDiagnostic(Rule, problematicAttribute.AttributeClass.Name, exportedType.Name));
            }
            else
            {
                // Attribute '{0}' comes from a different version of MEF than the export attribute on '{1}'
                var diagnostic = problematicAttribute.ApplicationSyntaxReference.CreateDiagnostic(
                    Rule, symbolContext.CancellationToken, problematicAttribute.AttributeClass.Name, exportedType.Name);
                symbolContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
