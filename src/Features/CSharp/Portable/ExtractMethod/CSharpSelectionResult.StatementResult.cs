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
using Roslyn.Utilities;

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
            {
                var node = GetContainingScope();

                return node switch
                {
                    AccessorDeclarationSyntax _ => false,
                    MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                    ParenthesizedLambdaExpressionSyntax lambda => lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword,
                    SimpleLambdaExpressionSyntax lambda => lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword,
                    AnonymousMethodExpressionSyntax anonymous => anonymous.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword,
                    _ => false,
                };
            }

            public override SyntaxNode GetContainingScope()
            {
                Contract.ThrowIfNull(SemanticDocument);
                Contract.ThrowIfTrue(IsExtractMethodOnExpression);

                return GetFirstTokenInSelection().GetRequiredParent().AncestorsAndSelf().First(n =>
                    n is AccessorDeclarationSyntax or
                         LocalFunctionStatementSyntax or
                         BaseMethodDeclarationSyntax or
                         AccessorDeclarationSyntax or
                         AnonymousFunctionExpressionSyntax or
                         CompilationUnitSyntax);
            }

            public override (ITypeSymbol? returnType, bool returnsByRef) GetReturnTypeInfo(CancellationToken cancellationToken)
            {
                Contract.ThrowIfTrue(IsExtractMethodOnExpression);

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

                    case MethodDeclarationSyntax methodDeclaration:
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
        }
    }
}
