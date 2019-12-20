// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class DiscardSyntaxClassifier : AbstractNameSyntaxClassifier
    {
        protected override int? GetRightmostNameArity(SyntaxNode node)
        {
            return null;
        }

        protected override bool IsParentAnAttribute(SyntaxNode node)
        {
            return node.IsParentKind(SyntaxKind.Attribute);
        }

        public override void AddClassifications(
           Workspace workspace,
           SyntaxNode syntax,
           SemanticModel semanticModel,
           ArrayBuilder<ClassifiedSpan> result,
           CancellationToken cancellationToken)
        {
            if (syntax.IsKind(SyntaxKind.DiscardDesignation) || syntax.IsKind(SyntaxKind.DiscardPattern))
            {
                result.Add(new ClassifiedSpan(syntax.Span, ClassificationTypeNames.Keyword));
                return;
            }

            var _ =
                TryClassifyCallArgument(syntax, semanticModel, result, cancellationToken) ||
                TryClassifyLambdaParameter(syntax, semanticModel, result, cancellationToken) ||
                TryClassifySymbol(syntax, semanticModel, result, cancellationToken);
        }

        private bool TryClassifyCallArgument(SyntaxNode syntax, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (!(syntax is ArgumentSyntax argument))
            {
                return false;
            }

            // in arguments we don't need to go on if the out is missing or the argument isn't inside a tuple
            if (!argument.RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword) &&
                !argument.IsParentKind(SyntaxKind.TupleExpression))
            {
                return false;
            }

            var operation = semanticModel.GetOperation(argument.Expression, cancellationToken);
            if (!(operation is IDiscardOperation discardOperation))
            {
                return false;
            }

            switch (discardOperation.Syntax)
            {
                case DeclarationExpressionSyntax decl:
                    result.Add(new ClassifiedSpan(decl.Designation.Span, ClassificationTypeNames.Keyword));
                    return true;
                case IdentifierNameSyntax discard:
                    result.Add(new ClassifiedSpan(discard.Span, ClassificationTypeNames.Keyword));
                    return true;
                default:
                    return false;
            }
        }

        private bool TryClassifyLambdaParameter(SyntaxNode syntax, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            if (!(syntax is LambdaExpressionSyntax lambda))
            {
                return false;
            }

            var symbolInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);
            var symbol = TryGetSymbol(lambda, symbolInfo, semanticModel) as IMethodSymbol;

            if (symbol == null)
            {
                return false;
            }

            DataFlowAnalysis? _lazyAnalysis = null;

            foreach (var parameter in symbol.Parameters)
            {
                if (parameter.IsDiscard)
                {
                    result.Add(new ClassifiedSpan(parameter.Locations[0].SourceSpan, ClassificationTypeNames.Keyword));
                }
                else if (parameter.Name == "_") //unfortunately parameter.IsDiscard currently doesn't work for all cases
                {
                    if (_lazyAnalysis == null)
                    {
                        _lazyAnalysis = semanticModel.AnalyzeDataFlow(lambda.Body);
                    }

                    //make sure the parameter isn't used in the lambda, in which case it wouldn't be discarding
                    if (!_lazyAnalysis.ReadInside.Contains(parameter))
                    {
                        result.Add(new ClassifiedSpan(parameter.Locations[0].SourceSpan, ClassificationTypeNames.Keyword));
                    }
                }
            }

            return true;
        }

        private bool TryClassifySymbol(SyntaxNode syntax, SemanticModel semanticModel, ArrayBuilder<ClassifiedSpan> result, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(syntax, cancellationToken);
            var symbol = TryGetSymbol(syntax, symbolInfo, semanticModel);

            if (symbol?.Kind == SymbolKind.Discard)
            {
                result.Add(new ClassifiedSpan(syntax.Span, ClassificationTypeNames.Keyword));
                return true;
            }

            return false;
        }
    }
}
