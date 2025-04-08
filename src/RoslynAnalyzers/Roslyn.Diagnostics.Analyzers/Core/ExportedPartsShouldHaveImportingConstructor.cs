// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

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
    using static RoslynDiagnosticsAnalyzersResources;

    /// <summary>
    /// RS0034: <inheritdoc cref="ExportedPartsShouldHaveImportingConstructorTitle"/>
    /// MEF-exported types should have exactly one constructor, which should be explicitly defined and marked with
    /// <see cref="ImportingConstructorAttribute"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ExportedPartsShouldHaveImportingConstructor : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.ExportedPartsShouldHaveImportingConstructorRuleId,
            CreateLocalizableResourceString(nameof(ExportedPartsShouldHaveImportingConstructorTitle)),
            CreateLocalizableResourceString(nameof(ExportedPartsShouldHaveImportingConstructorMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(ExportedPartsShouldHaveImportingConstructorDescription)),
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

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol? exportAttribute, INamedTypeSymbol? importingConstructorAttribute, INamedTypeSymbol namedType, IEnumerable<AttributeData> exportAttributes)
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

            IMethodSymbol? importingConstructor = null;
            var nonImportingConstructors = ImmutableArray<IMethodSymbol>.Empty;
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
                        context.ReportDiagnostic(
                            exportAttributeApplication.ApplicationSyntaxReference.CreateDiagnostic(
                                Rule, ScenarioProperties.ImplicitConstructor, context.CancellationToken, namedType.Name));
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
                    context.ReportDiagnostic(
                        appliedImportingConstructorAttribute.ApplicationSyntaxReference.CreateDiagnostic(
                            Rule, ScenarioProperties.NonPublicConstructor, context.CancellationToken, namedType.Name));
                    continue;
                }
            }

            IMethodSymbol? missingImportingConstructor = null;
            if (importingConstructor is null)
            {
                missingImportingConstructor = nonImportingConstructors.FirstOrDefault(constructor => constructor.DeclaredAccessibility == Accessibility.Public)
                    ?? nonImportingConstructors.FirstOrDefault();
            }

            foreach (var constructor in nonImportingConstructors)
            {
                var properties = Equals(constructor, missingImportingConstructor) ? ScenarioProperties.MissingAttribute : ScenarioProperties.MultipleConstructors;

                // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                context.ReportDiagnostic(constructor.DeclaringSyntaxReferences.CreateDiagnostic(Rule, properties, context.CancellationToken, namedType.Name));
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
            public static readonly ImmutableDictionary<string, string?> ImplicitConstructor = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(ImplicitConstructor));
            public static readonly ImmutableDictionary<string, string?> NonPublicConstructor = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(NonPublicConstructor));
            public static readonly ImmutableDictionary<string, string?> MissingAttribute = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(MissingAttribute));
            public static readonly ImmutableDictionary<string, string?> MultipleConstructors = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(MultipleConstructors));
        }
    }
}
