using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Roslyn.Diagnostics.Analyzers.MetaAnalyzers
{
    public abstract class RegisterActionAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax, TLanguageKindEnum> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TLanguageKindEnum : struct
    {
        internal static readonly string AnalysisContextFullName = typeof(AnalysisContext).FullName;
        internal static readonly string CompilationStartAnalysisContextFullName = typeof(CompilationStartAnalysisContext).FullName;
        internal static readonly string CodeBlockStartAnalysisContextFullName = typeof(CodeBlockStartAnalysisContext<>).FullName;
        internal static readonly string SymbolKindFullName = typeof(SymbolKind).FullName;

        private static LocalizableString localizableTitleMissingKindArgument = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MissingKindArgumentToRegisterActionTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessageMissingKindArgument = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.MissingKindArgumentToRegisterActionMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        public static DiagnosticDescriptor MissingKindArgumentRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
            localizableTitleMissingKindArgument,
            localizableMessageMissingKindArgument,
            "AnalyzerCorrectness",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static LocalizableString localizableTitleUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionTitle), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));
        private static LocalizableString localizableMessageUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(RoslynDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionMessage), RoslynDiagnosticsResources.ResourceManager, typeof(RoslynDiagnosticsResources));

        public static DiagnosticDescriptor UnsupportedSymbolKindArgumentRule = new DiagnosticDescriptor(
            RoslynDiagnosticIds.UnsupportedSymbolKindArgumentRuleId,
            localizableTitleUnsupportedSymbolKindArgument,
            localizableMessageUnsupportedSymbolKindArgument,
            "AnalyzerCorrectness",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        protected override DiagnosticDescriptor Descriptor { get { return MissingKindArgumentRule; } }

        protected override CompilationAnalyzer GetCompilationAnalyzer(Compilation compilation, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute)
        {
            var analysisContext = compilation.GetTypeByMetadataName(AnalysisContextFullName);
            if (analysisContext == null)
            {
                return null;
            }

            var compilationStartAnalysisContext = compilation.GetTypeByMetadataName(CompilationStartAnalysisContextFullName);
            if (compilationStartAnalysisContext == null)
            {
                return null;
            }

            var codeBlockStartAnalysisContext = compilation.GetTypeByMetadataName(CodeBlockStartAnalysisContextFullName);
            if (codeBlockStartAnalysisContext == null)
            {
                return null;
            }

            var symbolKind = compilation.GetTypeByMetadataName(SymbolKindFullName);
            if (symbolKind == null)
            {
                return null;
            }

            return GetAnalyzer(analysisContext, compilationStartAnalysisContext, codeBlockStartAnalysisContext, symbolKind, diagnosticAnalyzer, diagnosticAnalyzerAttribute);
        }

        protected abstract RegisterActionCompilationAnalyzer GetAnalyzer(INamedTypeSymbol analysisContext, INamedTypeSymbol compilationStartAnalysisContext, INamedTypeSymbol codeBlockStartAnalysisContext, INamedTypeSymbol symbolKind, INamedTypeSymbol diagnosticAnalyzer, INamedTypeSymbol diagnosticAnalyzerAttribute);

        protected abstract class RegisterActionCompilationAnalyzer : CompilationAnalyzer
        {
            internal static readonly string RegisterSyntaxNodeActionName = nameof(AnalysisContext.RegisterSyntaxNodeAction);
            internal static readonly string RegisterSymbolActionName = nameof(AnalysisContext.RegisterSymbolAction);

            private readonly INamedTypeSymbol analysisContext;
            private readonly INamedTypeSymbol compilationStartAnalysisContext;
            private readonly INamedTypeSymbol codeBlockStartAnalysisContext;
            private readonly INamedTypeSymbol symbolKind;

            private static readonly ImmutableHashSet<string> supportedSymbolKinds =
                ImmutableHashSet.Create(
                    nameof(SymbolKind.Event),
                    nameof(SymbolKind.Field), 
                    nameof(SymbolKind.Method), 
                    nameof(SymbolKind.NamedType), 
                    nameof(SymbolKind.Namespace), 
                    nameof(SymbolKind.Property));

            public RegisterActionCompilationAnalyzer(
                INamedTypeSymbol analysisContext, 
                INamedTypeSymbol compilationStartAnalysisContext,
                INamedTypeSymbol codeBlockStartAnalysisContext,
                INamedTypeSymbol symbolKind,
                INamedTypeSymbol diagnosticAnalyzer, 
                INamedTypeSymbol diagnosticAnalyzerAttribute)
                : base(diagnosticAnalyzer, diagnosticAnalyzerAttribute)
            {
                this.analysisContext = analysisContext;
                this.compilationStartAnalysisContext = compilationStartAnalysisContext;
                this.codeBlockStartAnalysisContext = codeBlockStartAnalysisContext;
                this.symbolKind = symbolKind;
            }

            protected abstract IEnumerable<SyntaxNode> GetArgumentExpressions(TInvocationExpressionSyntax invocation);

            internal IEnumerable<TClassDeclarationSyntax> GetClassDeclarationNodes(INamedTypeSymbol namedType, CancellationToken cancellationToken)
            {
                foreach (var syntax in namedType.DeclaringSyntaxReferences.Select(s => s.GetSyntax(cancellationToken)))
                {
                    if (syntax != null)
                    {
                        var classDecl = syntax.FirstAncestorOrSelf<TClassDeclarationSyntax>(ascendOutOfTrivia: false);
                        if (classDecl != null)
                        {
                            yield return classDecl;
                        }
                    }
                }
            }

            protected override void AnalyzeDiagnosticAnalyzer(SymbolAnalysisContext symbolContext)
            {
                var namedType = (INamedTypeSymbol)symbolContext.Symbol;
                var classDecls = GetClassDeclarationNodes(namedType, symbolContext.CancellationToken);
                foreach (var classDecl in classDecls)
                {
                    var invocations = classDecl.DescendantNodes().OfType<TInvocationExpressionSyntax>();
                    if (invocations.Any())
                    {
                        var semanticModel = symbolContext.Compilation.GetSemanticModel(classDecl.SyntaxTree);
                        foreach (var invocation in invocations)
                        {
                            var symbol = semanticModel.GetSymbolInfo(invocation, symbolContext.CancellationToken).Symbol;
                            if (symbol != null)
                            {
                                var isRegisterSymbolAction = symbol.Name.Equals(RegisterSymbolActionName) &&
                                    symbol.Kind == SymbolKind.Method &&
                                    (symbol.ContainingType.Equals(analysisContext) || 
                                    symbol.ContainingType.Equals(compilationStartAnalysisContext));

                                var isRegisterSyntaxNodeAction = !isRegisterSymbolAction &&
                                    symbol.Name.Equals(RegisterSyntaxNodeActionName) &&
                                    symbol.Kind == SymbolKind.Method &&
                                    (symbol.ContainingType.Equals(analysisContext) || 
                                    symbol.ContainingType.Equals(compilationStartAnalysisContext) ||
                                    symbol.ContainingType.Equals(codeBlockStartAnalysisContext));

                                if (isRegisterSymbolAction || isRegisterSyntaxNodeAction)
                                {
                                    var method = (IMethodSymbol)symbol;
                                    if (method.Parameters.Length == 2 && method.Parameters[1].IsParams)
                                    {
                                        var arguments = GetArgumentExpressions(invocation);
                                        if (arguments != null)
                                        {
                                            var argumentCount = arguments.Count();
                                            if (argumentCount >= 1)
                                            {
                                                var type = semanticModel.GetTypeInfo(arguments.First(), symbolContext.CancellationToken).ConvertedType;
                                                if (type == null || type.Name.Equals(nameof(Action)))
                                                {
                                                    if (argumentCount == 1)
                                                    {
                                                        string arg1, arg2;
                                                        if (isRegisterSymbolAction)
                                                        {
                                                            arg1 = nameof(SymbolKind);
                                                            arg2 = "symbol";
                                                        }
                                                        else
                                                        {
                                                            arg1 = nameof(TLanguageKindEnum);
                                                            arg2 = "syntax";
                                                        }

                                                        var diagnostic = Diagnostic.Create(MissingKindArgumentRule, invocation.GetLocation(), arg1, arg2);
                                                        symbolContext.ReportDiagnostic(diagnostic);
                                                    }
                                                    else if (isRegisterSymbolAction)
                                                    {
                                                        foreach (var argument in arguments.Skip(1))
                                                        {
                                                            symbol = semanticModel.GetSymbolInfo(argument, symbolContext.CancellationToken).Symbol;
                                                            if (symbol != null &&
                                                                symbol.Kind == SymbolKind.Field &&
                                                                symbolKind.Equals(symbol.ContainingType) &&
                                                                !supportedSymbolKinds.Contains(symbol.Name))
                                                            {
                                                                var diagnostic = Diagnostic.Create(UnsupportedSymbolKindArgumentRule, argument.GetLocation(), symbol.Name);
                                                                symbolContext.ReportDiagnostic(diagnostic);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
