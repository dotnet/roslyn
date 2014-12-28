using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.MetaAnalyzers
{
    [DiagnosticAnalyzer]
    public sealed class DiagnosticAnalyzerAttributeAnalyzer : DiagnosticAnalyzerCorrectnessAnalyzer
    {
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MissingDiagnosticAnalyzerAttributeTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MissingAttributeMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources), DiagnosticAnalyzerTypeFullName);

        public static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.MissingDiagnosticAnalyzerAttributeRuleId,
            localizableTitle,
            localizableMessage,
            "AnalyzerCorrectness",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        protected override DiagnosticDescriptor Descriptor { get { return Rule; } }

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            return new AttributeAnalyzer(diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class AttributeAnalyzer : CompilationAnalyzer
        {
            public AttributeAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                if (namedType.IsAbstract)
                {
                    return;
                }

                var namedTypeAttributes = AttributeHelpers.GetApplicableAttributes(namedType);
                foreach (var attribute in namedTypeAttributes)
                {
                    if (AttributeHelpers.DerivesFrom(attribute.AttributeClass, DiagnosticAnalyzerAttribute))
                    {
                        return;
                    }
                }

                var diagnostic = Diagnostic.Create(Rule, namedType.Locations[0]);
                symbolContext.ReportDiagnostic(diagnostic);
            }
        }
    }
}
