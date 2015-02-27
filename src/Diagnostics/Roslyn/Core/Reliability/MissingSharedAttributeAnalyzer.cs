// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.Reliability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class MissingSharedAttributeAnalyzer : DiagnosticAnalyzer
    {
        private static LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MissingSharedAttributeDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString s_localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MissingSharedAttributeMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.MissingSharedAttributeRuleId,
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
                var exportAttribute = compilationContext.Compilation.GetTypeByMetadataName("System.Composition.ExportAttribute");

                if (exportAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing both MEFv2, so we're done
                    return;
                }

                compilationContext.RegisterSymbolAction(c => AnalyzeSymbol(c, exportAttribute), SymbolKind.NamedType);
            });
        }

        private void AnalyzeSymbol(SymbolAnalysisContext symbolContext, INamedTypeSymbol exportAttribute)
        {
            var namedType = (INamedTypeSymbol)symbolContext.Symbol;
            var namedTypeAttributes = AttributeHelpers.GetApplicableAttributes(namedType);

            var exportAttributeApplication = namedTypeAttributes.FirstOrDefault(ad => AttributeHelpers.DerivesFrom(ad.AttributeClass, exportAttribute));

            if (exportAttributeApplication != null)
            {
                if (!namedTypeAttributes.Any(ad => ad.AttributeClass.Name == "SharedAttribute" &&
                                                   ad.AttributeClass.ContainingNamespace.Equals(exportAttribute.ContainingNamespace)))
                {
                    symbolContext.ReportDiagnostic(Diagnostic.Create(Rule, exportAttributeApplication.ApplicationSyntaxReference.GetSyntax().GetLocation()));
                }
            }
        }
    }
}
