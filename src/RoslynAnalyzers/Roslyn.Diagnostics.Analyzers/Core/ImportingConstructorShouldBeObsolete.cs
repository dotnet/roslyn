// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable warnings

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
    /// RS0033: <inheritdoc cref="ImportingConstructorShouldBeObsoleteTitle"/>
    /// 
    /// The importing constructor for a MEF-exported type should be marked obsolete.
    ///
    /// <code>
    /// [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    /// </code>
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class ImportingConstructorShouldBeObsolete : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            RoslynDiagnosticIds.ImportingConstructorShouldBeObsoleteRuleId,
            CreateLocalizableResourceString(nameof(ImportingConstructorShouldBeObsoleteTitle)),
            CreateLocalizableResourceString(nameof(ImportingConstructorShouldBeObsoleteMessage)),
            DiagnosticCategory.RoslynDiagnosticsReliability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: CreateLocalizableResourceString(nameof(ImportingConstructorShouldBeObsoleteDescription)),
            helpLinkUri: null,
            customTags: WellKnownDiagnosticTagsExtensions.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var obsoleteAttribute = compilationContext.Compilation.GetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemObsoleteAttribute);
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

                compilationContext.RegisterSymbolAction(symbolContext =>
                {
                    var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                    var exportAttributes = namedType.GetApplicableExportAttributes(exportAttributeV1, exportAttributeV2, inheritedExportAttribute);

                    AnalyzeSymbolForAttribute(ref symbolContext, obsoleteAttribute, exportAttributeV1, importingConstructorAttributeV1, namedType, exportAttributes);
                    AnalyzeSymbolForAttribute(ref symbolContext, obsoleteAttribute, exportAttributeV2, importingConstructorAttributeV2, namedType, exportAttributes);
                }, SymbolKind.NamedType);
            });
        }

        private static void AnalyzeSymbolForAttribute(ref SymbolAnalysisContext context, INamedTypeSymbol? obsoleteAttribute, INamedTypeSymbol? exportAttribute, INamedTypeSymbol? importingConstructorAttribute, INamedTypeSymbol namedType, IEnumerable<AttributeData> exportAttributes)
        {
            if (exportAttribute is null)
            {
                return;
            }

            if (!exportAttributes.Any(ad => ad.AttributeClass.DerivesFrom(exportAttribute)))
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
                AttributeData? importingConstructorAttributeData = null;
                foreach (var attributeData in constructorAttributes)
                {
                    if (attributeData.AttributeClass.DerivesFrom(importingConstructorAttribute))
                    {
                        importingConstructorAttributeData = attributeData;
                        break;
                    }
                }

                if (importingConstructorAttributeData is null)
                {
                    // This constructor is not marked [ImportingConstructor]
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
                        if (attributeData.ConstructorArguments.IsEmpty)
                        {
                            // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                            context.ReportDiagnostic(attributeData.ApplicationSyntaxReference.CreateDiagnostic(Rule, ScenarioProperties.MissingDescription, context.CancellationToken, namedType.Name));
                            break;
                        }
                        else if (attributeData.ConstructorArguments.Length == 1)
                        {
                            // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                            context.ReportDiagnostic(attributeData.ApplicationSyntaxReference.CreateDiagnostic(Rule, ScenarioProperties.MissingError, context.CancellationToken, namedType.Name));
                            break;
                        }
                        else
                        {
                            // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                            context.ReportDiagnostic(attributeData.ApplicationSyntaxReference.CreateDiagnostic(Rule, context.CancellationToken, namedType.Name));
                            break;
                        }
                    }

                    if (!Equals(attributeData.ConstructorArguments[0].Value, "This exported object must be obtained through the MEF export provider."))
                    {
                        // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                        context.ReportDiagnostic(attributeData.ApplicationSyntaxReference.CreateDiagnostic(Rule, ScenarioProperties.IncorrectDescription, context.CancellationToken, namedType.Name));
                        break;
                    }

                    if (!Equals(attributeData.ConstructorArguments[1].Value, true))
                    {
                        // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                        context.ReportDiagnostic(attributeData.ApplicationSyntaxReference.CreateDiagnostic(Rule, ScenarioProperties.ErrorSetToFalse, context.CancellationToken, namedType.Name));
                        break;
                    }

                    break;
                }

                if (!foundObsoleteAttribute)
                {
                    // '{0}' is MEF-exported and should have a single importing constructor of the correct form
                    context.ReportDiagnostic(importingConstructorAttributeData.ApplicationSyntaxReference.CreateDiagnostic(Rule, ScenarioProperties.MissingAttribute, context.CancellationToken, namedType.Name));
                    break;
                }
            }
        }

        internal static class Scenario
        {
            public const string MissingAttribute = nameof(MissingAttribute);
            public const string MissingDescription = nameof(MissingDescription);
            public const string IncorrectDescription = nameof(IncorrectDescription);
            public const string MissingError = nameof(MissingError);
            public const string ErrorSetToFalse = nameof(ErrorSetToFalse);
        }

        private static class ScenarioProperties
        {
            public static readonly ImmutableDictionary<string, string?> MissingAttribute = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(MissingAttribute));
            public static readonly ImmutableDictionary<string, string?> MissingDescription = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(MissingDescription));
            public static readonly ImmutableDictionary<string, string?> IncorrectDescription = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(IncorrectDescription));
            public static readonly ImmutableDictionary<string, string?> MissingError = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(MissingError));
            public static readonly ImmutableDictionary<string, string?> ErrorSetToFalse = ImmutableDictionary.Create<string, string?>().Add(nameof(Scenario), nameof(ErrorSetToFalse));
        }
    }
}
