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
    /// MEF-exported types should have exactly one constructor, which should be explicitly defined and marked with
    /// <see cref="ImportingConstructorAttribute"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ExportedPartsShouldHaveImportingConstructor : DiagnosticAnalyzer
    {
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ExportedPartsShouldHaveImportingConstructorTitle), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        private static readonly LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ExportedPartsShouldHaveImportingConstructorMessage), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsAnalyzersResources.ExportedPartsShouldHaveImportingConstructorDescription), RoslynDiagnosticsAnalyzersResources.ResourceManager, typeof(RoslynDiagnosticsAnalyzersResources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.ExportedPartsShouldHaveImportingConstructorRuleId,
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

                    AnalyzeSymbolForAttribute(ref symbolContext, exportAttributeV1, importingConstructorAttributeV1, namedType, namedTypeAttributes);
                    AnalyzeSymbolForAttribute(ref symbolContext, exportAttributeV2, importingConstructorAttributeV2, namedType, namedTypeAttributes);
                }, SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol exportAttributeOpt, INamedTypeSymbol importingConstructorAttribute, INamedTypeSymbol namedType, IEnumerable<AttributeData> namedTypeAttributes)
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
                    // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                    context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                    continue;
                }

                var constructorAttributes = constructor.GetAttributes();
                if (!constructorAttributes.Any(ad => ad.AttributeClass.DerivesFrom(importingConstructorAttribute)))
                {
                    // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                    context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                    continue;
                }

                if (constructor.DeclaredAccessibility != Accessibility.Public)
                {
                    // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                    context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), namedType.Name));
                    continue;
                }
            }
        }
    }
}
