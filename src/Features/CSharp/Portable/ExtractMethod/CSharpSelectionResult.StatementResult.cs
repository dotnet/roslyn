// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private class StatementResult : CSharpSelectionResult
        {
            public StatementResult(
                OperationStatus status,
                TextSpan originalSpan,
                TextSpan finalSpan,
                OptionSet options,
                bool selectionInExpression,
                SemanticDocument document,
                SyntaxAnnotation firstTokenAnnotation,
                SyntaxAnnotation lastTokenAnnotation)
                : base(status, originalSpan, finalSpan, options, selectionInExpression, document, firstTokenAnnotation, lastTokenAnnotation)
            {
            }

            public override bool ContainingScopeHasAsyncKeyword()
            {
                var node = this.GetContainingScope();

                return node switch
                {
                    AccessorDeclarationSyntax access => false,
                    MethodDeclarationSyntax method => method.Modifiers.Any(SyntaxKind.AsyncKeyword),
                    ParenthesizedLambdaExpressionSyntax lambda => lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword,
                    SimpleLambdaExpressionSyntax lambda => lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword,
                    AnonymousMethodExpressionSyntax anonymous => anonymous.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword,
                    _ => false,
                };
            }

            public override SyntaxNode GetContainingScope()
            {
                Contract.ThrowIfNull(this.SemanticDocument);
                Contract.ThrowIfTrue(this.SelectionInExpression);

                // it contains statements
                var firstToken = this.GetFirstTokenInSelection();
                return firstToken.GetAncestors<SyntaxNode>().FirstOrDefault(n =>
                {
                    return n is LocalFunctionStatementSyntax ||
                           n is BaseMethodDeclarationSyntax ||
                           n is AccessorDeclarationSyntax ||
                           n is ParenthesizedLambdaExpressionSyntax ||
                           n is SimpleLambdaExpressionSyntax ||
                           n is AnonymousMethodExpressionSyntax ||
                           n is CompilationUnitSyntax;
                });
            }

            public override ITypeSymbol GetContainingScopeType()
            {
                Contract.ThrowIfTrue(this.SelectionInExpression);

                var node = this.GetContainingScope();
                var semanticModel = this.SemanticDocument.SemanticModel;

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
