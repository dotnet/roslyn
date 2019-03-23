// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers
{
    /// <summary>
    /// The importing constructor for a MEF-exported type should be marked obsolete.
    ///
    /// <code>
    /// [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    /// </code>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ImportingConstructorShouldBeObsolete : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ImportingConstructorShouldBeObsoleteDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.ImportingConstructorShouldBeObsoleteRuleId,
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
                var obsoleteAttribute = compilationContext.Compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
                var exportAttributeV1 = compilationContext.Compilation.GetTypeByMetadataName("System.ComponentModel.Composition.ExportAttribute");
                var importingConstructorAttributeV1 = compilationContext.Compilation.GetTypeByMetadataName("System.ComponentModel.Composition.ImportingConstructorAttribute");
                var exportAttributeV2 = compilationContext.Compilation.GetTypeByMetadataName("System.Composition.ExportAttribute");
                var importingConstructorAttributeV2 = compilationContext.Compilation.GetTypeByMetadataName("System.Composition.ImportingConstructorAttribute");

                if (exportAttributeV1 is null && exportAttributeV2 is null)
                {
                    // We don't need to check assemblies unless they're referencing MEF, so we're done
                    return;
                }

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    var namedTypeAttributes = namedType.GetApplicableAttributes();

                    AnalyzeSymbolForAttribute(ref symbolContext, obsoleteAttribute, exportAttributeV1, importingConstructorAttributeV1, namedType, namedTypeAttributes);
                    AnalyzeSymbolForAttribute(ref symbolContext, obsoleteAttribute, exportAttributeV2, importingConstructorAttributeV2, namedType, namedTypeAttributes);
                }, SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol obsoleteAttribute, INamedTypeSymbol exportAttributeOpt, INamedTypeSymbol importingConstructorAttribute, INamedTypeSymbol namedType, IEnumerable<AttributeData> namedTypeAttributes)
        {
            if (exportAttributeOpt is null)
            {
                return;
            }

            var exportAttributeApplication = namedTypeAttributes.FirstOrDefault(ad => ad.AttributeClass.DerivesFrom(exportAttributeOpt));
            if (exportAttributeApplication is null)
            {
                return;
            }

            foreach (var constructor in namedType.Constructors)
            {
                if (constructor.IsImplicitlyDeclared)
                {
                    continue;
                }

                var constructorAttributes = constructor.GetAttributes();
                if (!constructorAttributes.Any(ad => ad.AttributeClass.DerivesFrom(importingConstructorAttribute)))
                {
                    continue;
                }

                var foundObsoleteAttribute = false;
                foreach (var attributeData in constructorAttributes)
                {
                    if (!attributeData.AttributeClass.Equals(obsoleteAttribute))
                    {
                        continue;
                    }

                    foundObsoleteAttribute = true;
                    if (attributeData.ConstructorArguments.Length != 2)
                    {
                        // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                        context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                        break;
                    }

                    if (!Equals(attributeData.ConstructorArguments[0].Value, "This exported object must be obtained through the MEF export provider."))
                    {
                        // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                        context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                        break;
                    }

                    if (!Equals(attributeData.ConstructorArguments[1].Value, true))
                    {
                        // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                        context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                        break;
                    }

                    break;
                }

                if (!foundObsoleteAttribute)
                {
                    // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                    context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                    break;
                }
            }
        }
    }
}
