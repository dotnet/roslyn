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
    [DiagnosticAnalyzer]
    public class MissingSharedAttributeAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.MissingSharedAttributeRuleId,
            RoslynDiagnosticsResources.MissingSharedAttributeDescription,
            RoslynDiagnosticsResources.MissingSharedAttributeMessage,
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
