// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal interface ISyntaxClassificationService : ILanguageService
    {
        ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers();

        /// <inheritdoc cref="IClassificationService.AddLexicalClassifications"/>
        void AddLexicalClassifications(SourceText text,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        /// <inheritdoc cref="IClassificationService.AddSyntacticClassificationsAsync"/>
        void AddSyntacticClassifications(
            SyntaxNode root,
            TextSpan textSpan,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        /// <inheritdoc cref="IClassificationService.AddSemanticClassificationsAsync"/>
        Task AddSemanticClassificationsAsync(
            Document document,
            TextSpan textSpan,
            ClassificationOptions options,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            CancellationToken cancellationToken);

        /// <inheritdoc cref="AddSemanticClassificationsAsync"/>
        void AddSemanticClassifications(
            SemanticModel semanticModel,
            TextSpan textSpan,
            Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
            Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
            ArrayBuilder<ClassifiedSpan> result,
            ClassificationOptions options,
            CancellationToken cancellationToken);

        string? GetSyntacticClassificationForIdentifier(SyntaxToken identifier);

        /// <inheritdoc cref="IClassificationService.AdjustStaleClassification"/>
        ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan);

        /// <inheritdoc cref="IClassificationService.ComputeSyntacticChangeRangeAsync"/>
        TextChangeRange? ComputeSyntacticChangeRange(
            SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken);
    }
}
