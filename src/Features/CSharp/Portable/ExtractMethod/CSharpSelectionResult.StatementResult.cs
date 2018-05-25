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
                SyntaxAnnotation lastTokenAnnotation) :
                base(status, originalSpan, finalSpan, options, selectionInExpression, document, firstTokenAnnotation, lastTokenAnnotation)
            {
            }

            public override bool ContainingScopeHasAsyncKeyword()
            {
                var node = this.GetContainingScope();
                var semanticModel = this.SemanticDocument.SemanticModel;

                switch (node)
                {
                    case AccessorDeclarationSyntax access: return false;
                    case MethodDeclarationSyntax method: return method.Modifiers.Any(SyntaxKind.AsyncKeyword);
                    case ParenthesizedLambdaExpressionSyntax lambda: return lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword;
                    case SimpleLambdaExpressionSyntax lambda: return lambda.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword;
                    case AnonymousMethodExpressionSyntax anonymous: return anonymous.AsyncKeyword.Kind() == SyntaxKind.AsyncKeyword;
                    default: return false;
                }
            }

            public override SyntaxNode GetContainingScope()
            {
                Contract.ThrowIfNull(this.SemanticDocument);
                Contract.ThrowIfTrue(this.SelectionInExpression);

                // it contains statements
                var firstToken = this.GetFirstTokenInSelection();
                return firstToken.GetAncestors<SyntaxNode>().FirstOrDefault(n =>
                {
                    return n is BaseMethodDeclarationSyntax ||
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

                        switch (semanticModel.GetDeclaredSymbol(access.Parent.Parent))
                        {
                            case IPropertySymbol propertySymbol:
                                return propertySymbol.Type;

                            case IEventSymbol eventSymbol:
                                return eventSymbol.Type;

                            default:
                                return null;
                        }

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
