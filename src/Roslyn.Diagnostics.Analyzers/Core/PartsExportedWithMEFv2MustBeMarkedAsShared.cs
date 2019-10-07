// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    /// <summary>
    /// RS0023: Parts exported with MEFv2 must be marked as Shared
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class PartsExportedWithMEFv2MustBeMarkedAsSharedAnalyzer : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.PartsExportedWithMEFv2MustBeMarkedAsSharedDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(RoslynDiagnosticIds.MissingSharedAttributeRuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessage,
                                                                             DiagnosticCategory.RoslyDiagnosticsReliability,
                                                                             DiagnosticHelpers.DefaultDiagnosticSeverity,
                                                                             isEnabledByDefault: DiagnosticHelpers.EnabledByDefaultIfNotBuildingVSIX,
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
                var exportAttribute = compilationContext.Compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemCompositionExportAttribute);
                var attributeUsageAttribute = compilationContext.Compilation.GetTypeByMetadataName(WellKnownTypeNames.SystemAttributeUsageAttribute);

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

                    if (exportAttributeApplication != null)
                    {
                        if (!namedTypeAttributes.Any(ad => ad.AttributeClass.Name == "SharedAttribute" &&
                                                           ad.AttributeClass.ContainingNamespace.Equals(exportAttribute.ContainingNamespace)))
                        {
                            // '{0}' is exported with MEFv2 and hence must be marked as Shared
                            symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                        }
                    }
                }, SymbolKind.NamedType);
            });
        }
    }
}