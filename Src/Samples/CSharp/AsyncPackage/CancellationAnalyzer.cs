using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AsyncPackage
{
    /// <summary>
    /// This analyzer check to see if there are Cancellation Tokens that can be propagated through async method calls
    /// </summary>
    [DiagnosticAnalyzer]
    [ExportDiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CancellationAnalyzer : ICodeBlockNestedAnalyzerFactory
    {
        internal const string CancellationId = "Async005";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(id: CancellationId,
            title: "Propagate CancellationTokens When Possible",
            messageFormat: "This method can take a CancellationToken",
            category: "Library",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public IDiagnosticAnalyzer CreateAnalyzerWithinCodeBlock(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, AnalyzerOptions options, CancellationToken cancellationToken)
        {
            var methodDeclaration = ownerSymbol as IMethodSymbol;

            if (methodDeclaration != null)
            {
                ITypeSymbol cancellationTokenType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                var paramTypes = methodDeclaration.Parameters.Select(x => x.Type);

                if (paramTypes.Contains(cancellationTokenType))
                {
                    // Analyze the inside of the code block for invocationexpressions
                    return new CancellationAnalyzer_Inner();
                }
            }

            return null;
        }

        internal class CancellationAnalyzer_Inner : ISyntaxNodeAnalyzer<SyntaxKind>
        {
            public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(CancellationAnalyzer.Rule); } }
            public ImmutableArray<SyntaxKind> SyntaxKindsOfInterest { get { return ImmutableArray.Create(SyntaxKind.InvocationExpression); } }

            public void AnalyzeNode(SyntaxNode node, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, AnalyzerOptions options, CancellationToken cancellationToken)
            {
                var invokeMethod = semanticModel.GetSymbolInfo(node).Symbol as IMethodSymbol;

                if (invokeMethod != null)
                {
                    ITypeSymbol cancellationTokenType = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken");

                    var invokeParams = invokeMethod.Parameters.Select(x => x.Type);

                    if (invokeParams.Contains(cancellationTokenType))
                    {
                        var passedToken = false;

                        foreach (var arg in ((InvocationExpressionSyntax)node).ArgumentList.Arguments)
                        {
                            var thisArgType = semanticModel.GetTypeInfo(arg.Expression).Type;

                            if (thisArgType != null && thisArgType.Equals(cancellationTokenType))
                            {
                                passedToken = true;
                            }
                        }

                        if (!passedToken)
                        {
                            addDiagnostic(Diagnostic.Create(CancellationAnalyzer.Rule, node.GetLocation()));
                        }
                    }
                }
            }

            public void OnCodeBlockEnded(SyntaxNode codeBlock, ISymbol ownerSymbol, SemanticModel semanticModel, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken)
            {
                return;
            }
        }
    }
}
