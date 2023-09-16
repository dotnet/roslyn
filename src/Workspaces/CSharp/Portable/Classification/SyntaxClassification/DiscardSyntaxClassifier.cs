// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class DiscardSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(
            typeof(DiscardDesignationSyntax),
            typeof(DiscardPatternSyntax),
            typeof(ParameterSyntax),
            typeof(IdentifierNameSyntax));

        public override void AddClassifications(
           SyntaxNode syntax,
           TextSpan textSpan,
           SemanticModel semanticModel,
           ClassificationOptions options,
           SegmentedList<ClassifiedSpan> result,
           CancellationToken cancellationToken)
        {
            if (syntax.Kind() is SyntaxKind.DiscardDesignation or SyntaxKind.DiscardPattern)
            {
                result.Add(new ClassifiedSpan(syntax.Span, ClassificationTypeNames.Keyword));
                return;
            }

            switch (syntax)
            {
                case ParameterSyntax parameter when parameter.Identifier.Text == "_":
                    var symbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken);

                    if (symbol?.IsDiscard == true)
                    {
                        result.Add(new ClassifiedSpan(parameter.Identifier.Span, ClassificationTypeNames.Keyword));
                    }

                    break;

                case IdentifierNameSyntax identifierName when identifierName.Identifier.Text == "_":
                    var symbolInfo = semanticModel.GetSymbolInfo(identifierName, cancellationToken);

                    if (symbolInfo.Symbol?.Kind == SymbolKind.Discard)
                    {
                        result.Add(new ClassifiedSpan(syntax.Span, ClassificationTypeNames.Keyword));
                    }

                    break;
            }
        }
    }
}
