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

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers;

internal class UsingDirectiveSyntaxClassifier : AbstractSyntaxClassifier
{
    public override void AddClassifications(
        SyntaxNode syntax,
        TextSpan textSpan,
        SemanticModel semanticModel,
        ClassificationOptions options,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        if (syntax is UsingDirectiveSyntax usingDirective)
        {
            ClassifyUsingDirectiveSyntax(usingDirective, semanticModel, result, cancellationToken);
        }
    }

    public override ImmutableArray<Type> SyntaxNodeTypes { get; } = [typeof(UsingDirectiveSyntax)];

    private static void ClassifyUsingDirectiveSyntax(
        UsingDirectiveSyntax usingDirective,
        SemanticModel semanticModel,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        // For using aliases, we bind the target on the right of the equals and use that
        // binding to classify the alias.
        if (usingDirective.Alias != null)
        {
            var token = usingDirective.Alias.Name;

            var symbolInfo = semanticModel.GetSymbolInfo(usingDirective.NamespaceOrType, cancellationToken);
            if (symbolInfo.Symbol is ITypeSymbol typeSymbol)
            {
                var classification = GetClassificationForType(typeSymbol);
                if (classification != null)
                {
                    result.Add(new ClassifiedSpan(token.Span, classification));
                }
            }
            else if (symbolInfo.Symbol?.Kind == SymbolKind.Namespace)
            {
                result.Add(new ClassifiedSpan(token.Span, ClassificationTypeNames.NamespaceName));
            }
        }
    }
}
