// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Simplification
{
    internal partial class CSharpMiscellaneousReducer : AbstractCSharpReducer
    {
        private static readonly ObjectPool<IReductionRewriter> s_pool = new ObjectPool<IReductionRewriter>(
            () => new Rewriter(s_pool));

        public CSharpMiscellaneousReducer() : base(s_pool)
        {
        }

        private static bool CanRemoveTypeFromParameter(
            ParameterSyntax parameterSyntax,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            // We reduce any the parameters that are contained inside ParameterList
            if (parameterSyntax.IsParentKind(SyntaxKind.ParameterList) &&
                parameterSyntax.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                if (parameterSyntax.Type != null)
                {
                    var annotation = new SyntaxAnnotation();
                    var newParameterSyntax = parameterSyntax.WithType(null).WithAdditionalAnnotations(annotation);

                    var oldLambda = parameterSyntax.FirstAncestorOrSelf<ParenthesizedLambdaExpressionSyntax>();
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

        private static Func<ParameterSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyParameter = SimplifyParameter;

        private static SyntaxNode SimplifyParameter(
            ParameterSyntax node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            if (CanRemoveTypeFromParameter(node, semanticModel, cancellationToken))
            {
                var newParameterSyntax = node.WithType(null);
                newParameterSyntax = SimplificationHelpers.CopyAnnotations(node, newParameterSyntax).WithoutAnnotations(Simplifier.Annotation);
                return newParameterSyntax;
            }

            return node;
        }

        private static readonly Func<ParenthesizedLambdaExpressionSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyParenthesizedLambdaExpression = SimplifyParenthesizedLambdaExpression;

        private static SyntaxNode SimplifyParenthesizedLambdaExpression(
            ParenthesizedLambdaExpressionSyntax parenthesizedLambda,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
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
                        parenthesizedLambda.Body);

                    return SimplificationHelpers.CopyAnnotations(parenthesizedLambda, newSimpleLambda).WithoutAnnotations(Simplifier.Annotation);
                }
            }

            return parenthesizedLambda;
        }

        private static readonly Func<BlockSyntax, SemanticModel, OptionSet, CancellationToken, SyntaxNode> s_simplifyBlock = SimplifyBlock;

        private static SyntaxNode SimplifyBlock(
            BlockSyntax node,
            SemanticModel semanticModel,
            OptionSet optionSet,
            CancellationToken cancellationToken)
        {
            if (node.Statements.Count == 1 &&
                CanHaveEmbeddedStatement(node.Parent) &&
                !optionSet.GetOption(CSharpCodeStyleOptions.PreferBraces).Value)
            {
                return node.Statements[0];
            }

            return node;
        }

        private static bool CanHaveEmbeddedStatement(SyntaxNode node)
        {
            if (node != null)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IfStatement:
                    case SyntaxKind.ElseClause:
                    case SyntaxKind.ForStatement:
                    case SyntaxKind.ForEachStatement:
                    case SyntaxKind.ForEachVariableStatement:
                    case SyntaxKind.WhileStatement:
                    case SyntaxKind.DoStatement:
                    case SyntaxKind.UsingStatement:
                    case SyntaxKind.LockStatement:
                        return true;
                }
            }

            return false;
        }
    }
}
