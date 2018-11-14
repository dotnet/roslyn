// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        private void ClassifyUsingDirectiveSyntax(
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

                var typeInfo = semanticModel.GetTypeInfo(usingDirective.Name, cancellationToken);
                if (typeInfo.Type != null)
                {
                    var classification = GetClassificationForType(typeInfo.Type);
                    if (classification != null)
                    {
                        result.Add(new ClassifiedSpan(token.Span, classification));
                    }
                    return;
                }

                // Since namespaces are not types, check the Symbol for how to classify the alias
                var symbolInfo = semanticModel.GetSymbolInfo(usingDirective.Name, cancellationToken);
                if (symbolInfo.Symbol?.Kind == SymbolKind.Namespace)
                {
                    result.Add(new ClassifiedSpan(token.Span, ClassificationTypeNames.NamespaceName));
                    return;
                }
            }
        }
    }
}
