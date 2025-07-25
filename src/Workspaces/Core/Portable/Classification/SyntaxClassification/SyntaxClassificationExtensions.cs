// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal static class SyntaxClassificationServiceExtensions
{
    extension(ISyntaxClassificationService classificationService)
    {
        public void AddSyntacticClassifications(
        SyntaxNode root,
        TextSpan textSpan,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
        {
            classificationService.AddSyntacticClassifications(root, [textSpan], result, cancellationToken);
        }

        public Task AddSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            SegmentedList<ClassifiedSpan> result,
            CancellationToken cancellationToken)
        {
            return classificationService.AddSemanticClassificationsAsync(
                document,
                [textSpan],
                options,
                getNodeClassifiers,
                getTokenClassifiers,
                result,
                cancellationToken);
        }

        public void AddSemanticClassifications(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            SegmentedList<ClassifiedSpan> result,
            ClassificationOptions options,
            CancellationToken cancellationToken)
        {
            classificationService.AddSemanticClassifications(
                semanticModel,
                [textSpan],
                getNodeClassifiers,
                getTokenClassifiers,
                result,
                options,
                cancellationToken);
        }
    }
}
