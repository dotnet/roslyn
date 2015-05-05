// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpMiscellaneousReducer : AbstractCSharpReducer
    {
        public override IExpressionRewriter CreateExpressionRewriter(OptionSet optionSet, CancellationToken cancellationToken)
        {
            return new Rewriter(optionSet, cancellationToken);
        }

        private static bool CanRemoveTypeFromParameter(
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // We reduce any the parameters that are contained inside ParameterList
            if (node != null && node.IsParentKind(SyntaxKind.ParameterList) && node.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                var parameterSyntax = (ParameterSyntax)node;
                if (parameterSyntax.Type != null)
                {
                    var annotation = new SyntaxAnnotation();
                    var newParameterSyntax = parameterSyntax.WithType(null).WithAdditionalAnnotations(annotation);

                    var oldLambda = node.FirstAncestorOrSelf<ParenthesizedLambdaExpressionSyntax>();
                    var newLambda = oldLambda.ReplaceNode(parameterSyntax, newParameterSyntax);
                    var speculationAnalyzer = new SpeculationAnalyzer(oldLambda, newLambda, semanticModel, cancellationToken);
                    newParameterSyntax = (ParameterSyntax)speculationAnalyzer.ReplacedExpression.GetAnnotatedNodesAndTokens(annotation).First();

                    var oldSymbol = semanticModel.GetDeclaredSymbol(parameterSyntax, cancellationToken);
                    var newSymbol = speculationAnalyzer.SpeculativeSemanticModel.GetDeclaredSymbol(newParameterSyntax, cancellationToken);
                    if (oldSymbol != null &&
                        newSymbol != null &&
                        oldSymbol.Type == newSymbol.Type)
                    {
                        return !speculationAnalyzer.ReplacementChangesSemantics();
                    }
                }
            }

            return false;
        }

        private static SyntaxNode SimplifyParameter(
            SyntaxNode node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            if (CanRemoveTypeFromParameter(node, semanticModel, cancellationToken))
            {
                var newParameterSyntax = ((ParameterSyntax)node).WithType(null);
                newParameterSyntax = SimplificationHelpers.CopyAnnotations(node, newParameterSyntax).WithoutAnnotations(Simplifier.Annotation);
                return newParameterSyntax;
            }

            return node;
        }

        private static SyntaxNode SimplifyParenthesizedLambdaExpression(
            SyntaxNode node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            if (node != null && node is ParenthesizedLambdaExpressionSyntax)
            {
                var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)node;
                if (parenthesizedLambda.ParameterList != null &&
                    parenthesizedLambda.ParameterList.Parameters.Count == 1)
                {
                    var parameter = parenthesizedLambda.ParameterList.Parameters.First();
                    if (CanRemoveTypeFromParameter(parameter, semanticModel, cancellationToken))
                    {
                        var newParameterSyntax = parameter.WithType(null);
                        var newSimpleLambda = SyntaxFactory.SimpleLambdaExpression(
                            parenthesizedLambda.AsyncKeyword,
                            newParameterSyntax.WithTrailingTrivia(parenthesizedLambda.ParameterList.GetTrailingTrivia()),
                            parenthesizedLambda.ArrowToken,
                            parenthesizedLambda.RefKeyword,
                            parenthesizedLambda.Body);

                        return SimplificationHelpers.CopyAnnotations(parenthesizedLambda, newSimpleLambda).WithoutAnnotations(Simplifier.Annotation);
                    }
                }
            }

            return node;
        }
    }
}
