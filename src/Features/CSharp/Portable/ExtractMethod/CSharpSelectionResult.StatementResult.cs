// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod;

internal sealed partial class CSharpExtractMethodService
{
    internal abstract partial class CSharpSelectionResult
    {
        /// <summary>
        /// Used when extracting either a single statement, or multiple statements to extract.
        /// </summary>
        private sealed class StatementResult(
            SemanticDocument document,
            SelectionType selectionType,
            TextSpan finalSpan)
            : CSharpSelectionResult(document, selectionType, finalSpan)
        {
            public override bool ContainingScopeHasAsyncKeyword()
                => GetContainingScope() switch
                {
                    MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                    LocalFunctionStatementSyntax localFunction => localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword),
                    AnonymousFunctionExpressionSyntax anonymousFunction => anonymousFunction.AsyncKeyword != default,
                    _ => false,
                };

            public override SyntaxNode GetContainingScope()
            {
                Contract.ThrowIfTrue(IsExtractMethodOnExpression);

                return GetFirstTokenInSelection().GetRequiredParent().AncestorsAndSelf().First(n =>
                    n is AccessorDeclarationSyntax or
                         LocalFunctionStatementSyntax or
                         BaseMethodDeclarationSyntax or
                         AnonymousFunctionExpressionSyntax or
                         CompilationUnitSyntax);
            }

            protected override (ITypeSymbol? returnType, bool returnsByRef) GetReturnTypeInfoWorker(CancellationToken cancellationToken)
            {
                var node = GetContainingScope();
                var semanticModel = SemanticDocument.SemanticModel;

                switch (node)
                {
                    case AccessorDeclarationSyntax access:
                        return semanticModel.GetDeclaredSymbol(access.GetRequiredParent().GetRequiredParent(), cancellationToken) switch
                        {
                            IPropertySymbol propertySymbol => (propertySymbol.Type, propertySymbol.ReturnsByRef),
                            IEventSymbol eventSymbol => (eventSymbol.Type, false),
                            _ => throw ExceptionUtilities.UnexpectedValue(node),
                        };

                    case LocalFunctionStatementSyntax localFunction:
                        {
                            var method = semanticModel.GetRequiredDeclaredSymbol(localFunction, cancellationToken);
                            return (method.ReturnType, method.ReturnsByRef);
                        }

                    case BaseMethodDeclarationSyntax methodDeclaration:
                        {
                            var method = semanticModel.GetRequiredDeclaredSymbol(methodDeclaration, cancellationToken);
                            return (method.ReturnType, method.ReturnsByRef);
                        }

                    case AnonymousFunctionExpressionSyntax function:
                        {
                            return semanticModel.GetSymbolInfo(function, cancellationToken).Symbol is not IMethodSymbol method
                                ? default
                                : (method.ReturnType, method.ReturnsByRef);
                        }

                    default:
                        return default;
                }
            }

            public override SyntaxNode GetOutermostCallSiteContainerToProcess(CancellationToken cancellationToken)
            {
                if (this.IsExtractMethodOnSingleStatement)
                {
                    var firstStatement = this.GetFirstStatement();
                    return firstStatement.GetRequiredParent();
                }

                if (this.IsExtractMethodOnMultipleStatements)
                {
                    var firstStatement = this.GetFirstStatementUnderContainer();
                    var container = firstStatement.GetRequiredParent();
                    return container is GlobalStatementSyntax ? container.GetRequiredParent() : container;
                }

                throw ExceptionUtilities.Unreachable();
            }
        }
    }
}
