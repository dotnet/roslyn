using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Analyzers.MetaAnalyzers
{
    public abstract class RegisterActionAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax, TLanguageKindEnum> : DiagnosticAnalyzerCorrectnessAnalyzer
        where TClassDeclarationSyntax : SyntaxNode
        where TInvocationExpressionSyntax : SyntaxNode
        where TLanguageKindEnum : struct
    {
        private static LocalizableString localizableTitleMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableMessageMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableDescriptionMissingKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.MissingKindArgumentToRegisterActionDescription), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor MissingKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.MissingKindArgumentToRegisterActionRuleId,
            localizableTitleMissingKindArgument,
            localizableMessageMissingKindArgument,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: localizableDescriptionMissingKindArgument,
            customTags: WellKnownDiagnosticTags.Telemetry);

        private static LocalizableString localizableTitleUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionTitle), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));
        private static LocalizableString localizableMessageUnsupportedSymbolKindArgument = new LocalizableResourceString(nameof(CodeAnalysisDiagnosticsResources.UnsupportedSymbolKindArgumentToRegisterActionMessage), CodeAnalysisDiagnosticsResources.ResourceManager, typeof(CodeAnalysisDiagnosticsResources));

        public static DiagnosticDescriptor UnsupportedSymbolKindArgumentRule = new DiagnosticDescriptor(
            DiagnosticIds.UnsupportedSymbolKindArgumentRuleId,
            localizableTitleUnsupportedSymbolKindArgument,
            localizableMessageUnsupportedSymbolKindArgument,
            DiagnosticCategory.AnalyzerCorrectness,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(MissingKindArgumentRule, UnsupportedSymbolKindArgumentRule);
            }
        }

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

        protected abstract class RegisterActionCompilationAnalyzer : InvocationCompilationAnalyzer<TClassDeclarationSyntax, TInvocationExpressionSyntax>
        {
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

            protected override void AnalyzeInvocation(SymbolAnalysisContext symbolContext, TInvocationExpressionSyntax invocation, ISymbol symbol, SemanticModel semanticModel)
            {
                var isRegisterSymbolAction = symbol.Name.Equals(RegisterSymbolActionName, StringComparison.OrdinalIgnoreCase) &&
                                    symbol.Kind == SymbolKind.Method &&
                                    (symbol.ContainingType.Equals(analysisContext) ||
                                    symbol.ContainingType.Equals(compilationStartAnalysisContext));

                var isRegisterSyntaxNodeAction = !isRegisterSymbolAction &&
                    symbol.Name.Equals(RegisterSyntaxNodeActionName, StringComparison.OrdinalIgnoreCase) &&
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
                                            arg1 = "SyntaxKind";
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
