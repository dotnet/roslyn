using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Roslyn.Diagnostics.Analyzers.MetaAnalyzers
{
    public abstract class DiagnosticAnalyzerCorrectnessAnalyzer : DiagnosticAnalyzer
    {
        protected static readonly string DiagnosticAnalyzerTypeFullName = typeof(DiagnosticAnalyzer).FullName;
        internal static readonly string DiagnosticAnalyzerAttributeFullName = typeof(DiagnosticAnalyzerAttribute).FullName;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(Descriptor);
            }
        }

        protected abstract DiagnosticDescriptor Descriptor { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(compilationContext =>
            {
                var diagnosticAnalyzer = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerTypeFullName);
                var diagnosticAnalyzerAttribute = compilationContext.Compilation.GetTypeByMetadataName(DiagnosticAnalyzerAttributeFullName);

                if (diagnosticAnalyzer == null || diagnosticAnalyzerAttribute == null)
                {
                    // We don't need to check assemblies unless they're referencing Microsoft.CodeAnalysis which defines DiagnosticAnalyzer.
                    return;
                }

                var analyzer = GetCompilationAnalyzer(compilationContext.Compilation, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
                if (analyzer == null)
                {
                    return;
                }

                compilationContext.RegisterSymbolAction(c => analyzer.AnalyzeSymbol(c), SymbolKind.NamedType);
            });
        }

        protected abstract CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute);

        protected abstract class CompilationAnalyzer
        {
            private readonly INamedTypeSymbol diagnosticAnalyzer;
            private readonly INamedTypeSymbol diagnosticAnalyzerAttribute;

            public CompilationAnalyzer(INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
            {
                this.diagnosticAnalyzer = diagnosticAnalyzer;
                this.diagnosticAnalyzerAttribute = diagnosticAnalyzerAttribute;
            }

            protected INamedTypeSymbol DiagnosticAnalyzer { get { return this.diagnosticAnalyzer; } }
            protected INamedTypeSymbol DiagnosticAnalyzerAttribute { get { return this.diagnosticAnalyzerAttribute; } }

            protected bool IsDiagnosticAnalyzer(INamedTypeSymbol type)
            {
                return SymbolEquivalenceComparer.Instance.Equals(type, this.diagnosticAnalyzer);
            }

            internal void AnalyzeSymbol(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                if (namedType.GetBaseTypes().Any(IsDiagnosticAnalyzer))
                {
                    AnalyzeDiagnosticAnalyzer(symbolContext);
                }
            }

            protected abstract void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext);
        }
    }
}
