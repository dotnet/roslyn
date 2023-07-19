// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractMethod
{
    internal partial class CSharpSelectionResult
    {
        private class StatementResult(
            OperationStatus status,
            TextSpan originalSpan,
            TextSpan finalSpan,
            ExtractMethodOptions options,
            bool selectionInExpression,
            SemanticDocument document,
            SyntaxAnnotation firstTokenAnnotation,
            SyntaxAnnotation lastTokenAnnotation) : CSharpSelectionResult(status, originalSpan, finalSpan, options, selectionInExpression, document, firstTokenAnnotation, lastTokenAnnotation)
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
                Contract.ThrowIfTrue(SelectionInExpression);

                // it contains statements
                var firstToken = GetFirstTokenInSelection();
                return firstToken.GetAncestors<SyntaxNode>().FirstOrDefault(n =>
                {
                    return n is AccessorDeclarationSyntax or
                           LocalFunctionStatementSyntax or
                           BaseMethodDeclarationSyntax or
                           AccessorDeclarationSyntax or
                           ParenthesizedLambdaExpressionSyntax or
                           SimpleLambdaExpressionSyntax or
                           AnonymousMethodExpressionSyntax or
                           CompilationUnitSyntax;
                });
            }

            public override ITypeSymbol GetContainingScopeType()
            {
                Contract.ThrowIfTrue(SelectionInExpression);

                var node = GetContainingScope();
                var semanticModel = SemanticDocument.SemanticModel;

                switch (node)
                {
                    case AccessorDeclarationSyntax access:
                        // property or event case
                        if (access.Parent == null || access.Parent.Parent == null)
                        {
                            return null;
                        }

                        return semanticModel.GetDeclaredSymbol(access.Parent.Parent) switch
                        {
                            IPropertySymbol propertySymbol => propertySymbol.Type,
                            IEventSymbol eventSymbol => eventSymbol.Type,
                            _ => null,
                        };

                    case MethodDeclarationSyntax method:
                        return semanticModel.GetDeclaredSymbol(method).ReturnType;

                    case ParenthesizedLambdaExpressionSyntax lambda:
                        return semanticModel.GetLambdaOrAnonymousMethodReturnType(lambda);

                    case SimpleLambdaExpressionSyntax lambda:
                        return semanticModel.GetLambdaOrAnonymousMethodReturnType(lambda);

                    case AnonymousMethodExpressionSyntax anonymous:
                        return semanticModel.GetLambdaOrAnonymousMethodReturnType(anonymous);

                    default:
                        return null;
                }
            }
        }
    }
}
