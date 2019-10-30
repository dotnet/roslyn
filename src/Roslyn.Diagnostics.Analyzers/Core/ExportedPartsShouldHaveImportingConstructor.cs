// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
                var exportAttributeV1 = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionExportAttribute);
                var importingConstructorAttributeV1 = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionImportingConstructorAttribute);
                var exportAttributeV2 = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCompositionExportAttribute);
                var inheritedExportAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemComponentModelCompositionInheritedExportAttribute);
                var importingConstructorAttributeV2 = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCompositionImportingConstructorAttribute);

                if (exportAttributeV1 is null && exportAttributeV2 is null)
                {
                    // We don't need to check assemblies unless they're referencing MEF, so we're done
                    return;
                }

                if (exportAttributeV1 is object && importingConstructorAttributeV1 is null)
                {
                    throw new InvalidOperationException("Found MEF v1 ExportAttribute, but could not find the corresponding ImportingConstructorAttribute.");
                }

                if (exportAttributeV2 is object && importingConstructorAttributeV2 is null)
                {
                    throw new InvalidOperationException("Found MEF v2 ExportAttribute, but could not find the corresponding ImportingConstructorAttribute.");
                }

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    var exportAttributes = namedType.GetApplicableExportAttributes(exportAttributeV1, exportAttributeV2, inheritedExportAttribute);

                    AnalyzeSymbolForAttribute(ref symbolContext, exportAttributeV1, importingConstructorAttributeV1, namedType, exportAttributes);
                    AnalyzeSymbolForAttribute(ref symbolContext, exportAttributeV2, importingConstructorAttributeV2, namedType, exportAttributes);
                }, SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol exportAttributeOpt, INamedTypeSymbol importingConstructorAttribute, INamedTypeSymbol namedType, IEnumerable<AttributeData> exportAttributes)
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

            IMethodSymbol importingConstructor = null;
            ImmutableArray<IMethodSymbol> nonImportingConstructors = ImmutableArray<IMethodSymbol>.Empty;
            foreach (var constructor in namedType.Constructors)
            {
                if (constructor.IsStatic)
                {
                    // Ignore static constructors
                    continue;
                }

                if (constructor.IsImplicitlyDeclared)
                {
                    if (exportAttributeApplication.ApplicationSyntaxReference is object)
                    {
                        // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                        context.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation(), ScenarioProperties.ImplicitConstructor, namedType.Name));
                    }

                    continue;
                }

                var constructorAttributes = constructor.GetAttributes();
                var appliedImportingConstructorAttribute = constructorAttributes.FirstOrDefault(ad => ad.AttributeClass.DerivesFrom(importingConstructorAttribute));
                if (appliedImportingConstructorAttribute is null)
                {
                    nonImportingConstructors = nonImportingConstructors.Add(constructor);
                    continue;
                }

                importingConstructor = constructor;
                if (constructor.DeclaredAccessibility != Accessibility.Public)
                {
                    // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                    context.ReportDiagnostic(Diagnostic.Create(Rule, appliedImportingConstructorAttribute.ApplicationSyntaxReference.GetSyntax().GetLocation(), ScenarioProperties.NonPublicConstructor, namedType.Name));
                    continue;
                }
            }

            IMethodSymbol missingImportingConstructor = null;
            if (importingConstructor is null)
            {
                missingImportingConstructor = nonImportingConstructors.FirstOrDefault(constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                    ?? nonImportingConstructors.FirstOrDefault();
            }

            foreach (var constructor in nonImportingConstructors)
            {
                var properties = Equals(constructor, missingImportingConstructor) ? ScenarioProperties.MissingAttribute : ScenarioProperties.MultipleConstructors;

                // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                context.ReportDiagnostic(Diagnostic.Create(Rule, constructor.DeclaringSyntaxReferences.First().GetSyntax().GetLocation(), properties, namedType.Name));
                continue;
            }
        }

        internal static class Scenario
        {
            public const string ImplicitConstructor = nameof(ImplicitConstructor);
            public const string NonPublicConstructor = nameof(NonPublicConstructor);
            public const string MissingAttribute = nameof(MissingAttribute);
            public const string MultipleConstructors = nameof(MultipleConstructors);
        }

        private static class ScenarioProperties
        {
            public static readonly ImmutableDictionary<string, string> ImplicitConstructor = ImmutableDictionary.Create<string, string>().Add(nameof(Scenario), nameof(ImplicitConstructor));
            public static readonly ImmutableDictionary<string, string> NonPublicConstructor = ImmutableDictionary.Create<string, string>().Add(nameof(Scenario), nameof(NonPublicConstructor));
            public static readonly ImmutableDictionary<string, string> MissingAttribute = ImmutableDictionary.Create<string, string>().Add(nameof(Scenario), nameof(MissingAttribute));
            public static readonly ImmutableDictionary<string, string> MultipleConstructors = ImmutableDictionary.Create<string, string>().Add(nameof(Scenario), nameof(MultipleConstructors));
        }
    }
}
