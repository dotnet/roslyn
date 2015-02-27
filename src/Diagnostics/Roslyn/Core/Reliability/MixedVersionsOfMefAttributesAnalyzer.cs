// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.Reliability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class MixedVersionsOfMefAttributesAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string[] s_mefNamespaces = new[] { "System.ComponentModel.Composition", "System.Composition" };

        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MixedVersionsOfMefAttributesDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MixedVersionsOfMefAttributesMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.MixedVersionsOfMefAttributesRuleId,
            s_localizableTitle,
            s_localizableMessage,
            "Reliability",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Rule);
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
                {
                    var exportAttributes = new List<INamedTypeSymbol>();

                    foreach (var mefNamespace in s_mefNamespaces)
                    {
                        var exportAttribute = compilationContext.Compilation.GetTypeByMetadataName(mefNamespace + ".ExportAttribute");

                        if (exportAttribute == null)
                        {
                            // We don't need to check assemblies unless they're referencing both versions of MEF, so we're done
                            return;
                        }

                        exportAttributes.Add(exportAttribute);
                    }

                    compilationContext.RegisterSymbolAction(c => AnalyzeSymbol(c, exportAttributes), SymbolKind.NamedType);
                });
        }

        private void AnalyzeSymbol(SymbolAnalysisContext symbolContext, IEnumerable<INamedTypeSymbol> exportAttributes)
        {
            var namedType = (INamedTypeSymbol)symbolContext.Symbol;
            var namedTypeAttributes = AttributeHelpers.GetApplicableAttributes(namedType);

            // Figure out which export attributes are being used here
            var appliedExportAttributes = exportAttributes.Where(e => namedTypeAttributes.Any(ad => AttributeHelpers.DerivesFrom(ad.AttributeClass, e))).ToList();

            // If we have no exports we're done
            if (appliedExportAttributes.Count == 0)
            {
                return;
            }

            var badNamespaces = exportAttributes.Except(appliedExportAttributes).Select(s => s.ContainingNamespace).ToList();

            // Now look at all attributes and see if any are metadata attributes
            foreach (var namedTypeAttribute in namedTypeAttributes)
            {
                if (AttributeHelpers.GetApplicableAttributes(namedTypeAttribute.AttributeClass).Any(ad => badNamespaces.Contains(ad.AttributeClass.ContainingNamespace) &&
                                                                                                          ad.AttributeClass.Name == "MetadataAttributeAttribute"))
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
                var methodSymbol = member as IMethodSymbol;

                if (methodSymbol != null && methodSymbol.MethodKind == MethodKind.Constructor)
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
            var metadataSyntax = problematicAttribute.ApplicationSyntaxReference;
            var displayStringOfAttribute = problematicAttribute.AttributeClass.ToMinimalDisplayString(symbolContext.Compilation.GetSemanticModel(metadataSyntax.SyntaxTree),
                                                                                                      metadataSyntax.Span.Start,
                                                                                                      GetSymbolDisplayFormat(exportedType, minimal: true));

            var displayStringOfExport = exportedType.ToDisplayString(GetSymbolDisplayFormat(exportedType, minimal: false));

            symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, metadataSyntax.GetSyntax().GetLocation(), displayStringOfAttribute, displayStringOfExport));
        }

        private static SymbolDisplayFormat GetSymbolDisplayFormat(ISymbol symbol, bool minimal)
        {
            if (symbol.Language == LanguageNames.CSharp)
            {
                return minimal ? SymbolDisplayFormat.CSharpShortErrorMessageFormat : SymbolDisplayFormat.CSharpErrorMessageFormat;
            }
            else
            {
                return minimal ? SymbolDisplayFormat.VisualBasicShortErrorMessageFormat : SymbolDisplayFormat.VisualBasicErrorMessageFormat;
            }
        }
    }
}
