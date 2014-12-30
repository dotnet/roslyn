using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.MetaAnalyzers
{
    public abstract class CompilationEndActionAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
    {
        private static LocalizableString localizableTitle = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ExpensiveEndActionTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessage = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ExpensiveEndActionMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources), nameof(AnalysisContext), nameof(CompilationStartAnalysisContext));
        private static LocalizableString localizableDescription = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.ExpensiveEndActionDescription), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources), nameof(CompilationStartAnalysisContext), nameof(AnalysisContext), nameof(DiagnosticAnalyzer.Initialize));

        public static DiagnosticDescriptor ExpensiveCompilationEndActionRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.ExpensiveCompilationEndActionRuleId,
            localizableTitle,
            localizableMessage,
            "AnalyzerPerformance",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(ExpensiveCompilationEndActionRule);
            }
        }

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var compilationStartAnalysisContext = compilation.GetTypeByMetadataName(CompilationStartAnalysisContextFullName);
            if (compilationStartAnalysisContext == null)
            {
                return null;
            }

            return new EndActionCompilationAnalyzer(compilationStartAnalysisContext, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        private sealed class EndActionCompilationAnalyzer : InvocationCompilationAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax>
        {
            private readonly INamedTypeSymbol compilationStartAnalysisContext;

            private ImmutableDictionary<INamedTypeSymbol, ActionsInfo> actionsInfoMap;
            private struct ActionsInfo
            {
                public bool HasEndAction;
                public bool HasNonEndAction;
                public TInvocationExpressionSyntax EndActionInvocation;
            }

            public EndActionCompilationAnalyzer(INamedTypeSymbol compilationStartAnalysisContext, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                this.compilationStartAnalysisContext = compilationStartAnalysisContext;
                this.actionsInfoMap = ImmutableDictionary<INamedTypeSymbol, ActionsInfo>.Empty;
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                base.AnalyzeDiagnosticAnalyzer(symbolContext);

                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                ActionsInfo result;
                if (actionsInfoMap.TryGetValue(namedType, out result))
                {
                    if (result.HasEndAction && result.HasNonEndAction)
                    {
                        var diagnostic = Diagnostic.Create(ExpensiveCompilationEndActionRule, result.EndActionInvocation.GetLocation());
                        symbolContext.ReportDiagnostic(diagnostic);
                    }
                }
            }

            protected override void AnalyzeInvocation(SymbolAnalysisContext symbolContext, TInvocationExpressionSyntax invocation, ISymbol symbol, SemanticModel semanticModel)
            {
                if (!compilationStartAnalysisContext.Equals(symbol.ContainingType) || symbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                var isRegisterEndAction = symbol.Name.Equals(RegisterCompilationEndActionName, StringComparison.OrdinalIgnoreCase);
                var isRegisterNonEndAction = !isRegisterEndAction && symbol.Name.StartsWith("Register", StringComparison.OrdinalIgnoreCase);
                if (isRegisterEndAction || isRegisterNonEndAction)
                {
                    var result = ImmutableInterlocked.GetOrAdd(ref actionsInfoMap, (INamedTypeSymbol)symbolContext.Symbol, _ => new ActionsInfo());
                    result.HasEndAction |= isRegisterEndAction;
                    result.HasNonEndAction |= isRegisterNonEndAction;

                    if (isRegisterEndAction)
                    {
                        result.EndActionInvocation = invocation;
                    }
                }
            }
        }
    }
}
