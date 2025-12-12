// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

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
    /// RS0023: <inheritdoc cref="PartsExportedWithMEFv2MustBeMarkedAsSharedTitle"/>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.MissingSharedAttributeRuleId,
            CreateLocalizableResourceString(nameof(PartsExportedWithMEFv2MustBeMarkedAsSharedTitle)),
            CreateLocalizableResourceString(nameof(PartsExportedWithMEFv2MustBeMarkedAsSharedMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(PartsExportedWithMEFv2MustBeMarkedAsSharedDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var exportAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCompositionExportAttribute);
                var attributeUsageAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemAttributeUsageAttribute);

                if (exportAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing both MEFv2, so we're done
                    return;
                }

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    var namedTypeAttributes = namedType.GetApplicableAttributes(attributeUsageAttribute);
                    var exportAttributes = namedType.GetApplicableExportAttributes(exportAttributeV1: null, exportAttribute, inheritedExportAttribute: null);

                    var exportAttributeApplication = exportAttributes.FirstOrDefault();

                    if (exportAttributeApplication != null &&
                        !namedTypeAttributes.Any(ad => ad.AttributeClass.Name == "SharedAttribute" &&
                                                       ad.AttributeClass.ContainingNamespace.Equals(exportAttribute.ContainingNamespace)))
                    {
                        if (exportAttributeApplication.ApplicationSyntaxReference == null)
                        {
                            symbolContext.ReportDiagnostic(symbolContext.Symbol.CreateDiagnostic(Rule, namedType.Name));
                        }
                        else
                        {
                            // '{0}' is exported with MEFv2 and hence must be marked as Shared
                            symbolContext.ReportDiagnostic(exportAttributeApplication.ApplicationSyntaxReference.CreateDiagnostic(Rule, symbolContext.CancellationToken, namedType.Name));
                        }
                    }
                }, SymbolKind.NamedType);
            });
        }
    }
}
