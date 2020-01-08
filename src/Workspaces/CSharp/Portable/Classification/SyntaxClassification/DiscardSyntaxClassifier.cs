// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class DiscardSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(
            typeof(DiscardDesignationSyntax),
            typeof(DiscardPatternSyntax),
            typeof(LambdaExpressionSyntax),
            typeof(IdentifierNameSyntax));

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

            switch (syntax)
            {
                case LambdaExpressionSyntax lambda:
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);
                        ClassifyLambdaParameter(symbolInfo, result);
                    }
                    break;

                case IdentifierNameSyntax identifier:
                    if (identifier.Identifier.Text == "_")
                    {
                        var symbolInfo = semanticModel.GetSymbolInfo(identifier, cancellationToken);

                        if (symbolInfo.Symbol?.Kind == SymbolKind.Discard)
                        {
                            result.Add(new ClassifiedSpan(syntax.Span, ClassificationTypeNames.Keyword));
                        }
                    }
                    break;

                default:
                    throw ExceptionUtilities.Unreachable;
            };
        }

        private void ClassifyLambdaParameter(SymbolInfo symbolInfo, ArrayBuilder<ClassifiedSpan> result)
        {
            // classify lambda parameters of the forms 
            // (int _, int _) => ... 
            // or 
            // (_, _) => ...
            // which aren't covered by TryClassifySymbol

            if (!(symbolInfo.Symbol is IMethodSymbol symbol))
            {
                return;
            }

            foreach (var parameter in symbol.Parameters)
            {
                if (parameter.IsDiscard)
                {
                    result.Add(new ClassifiedSpan(parameter.Locations[0].SourceSpan, ClassificationTypeNames.Keyword));
                }
            }
        }
    }
}
