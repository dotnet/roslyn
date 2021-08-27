﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Classification.Classifiers
{
    internal class UsingDirectiveSyntaxClassifier : AbstractSyntaxClassifier
    {
        public override void AddClassifications(
            Workspace workspace,
            SyntaxNode syntax,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            if (syntax is UsingDirectiveSyntax usingDirective)
            {
                ClassifyUsingDirectiveSyntax(usingDirective, semanticModel, result, cancellationToken);
            }
        }

        public override ImmutableArray<Type> SyntaxNodeTypes { get; } = ImmutableArray.Create(typeof(UsingDirectiveSyntax));

        private static void ClassifyUsingDirectiveSyntax(
            UsingDirectiveSyntax usingDirective,
            SemanticModel semanticModel,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            // For using aliases, we bind the target on the right of the equals and use that
            // binding to classify the alias.
            if (usingDirective.Alias != null)
            {
                var token = usingDirective.Alias.Name;

                var symbolInfo = semanticModel.GetSymbolInfo(usingDirective.Name, cancellationToken);
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
}
