// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification.Classifiers;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Classification;

internal abstract partial class AbstractSyntaxClassificationService : ISyntaxClassificationService
{
    public abstract void AddLexicalClassifications(SourceText text, TextSpan textSpan, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken);
    public abstract void AddSyntacticClassifications(SyntaxNode root, ImmutableArray<TextSpan> textSpans, SegmentedList<ClassifiedSpan> result, CancellationToken cancellationToken);

    public abstract ImmutableArray<ISyntaxClassifier> GetDefaultSyntaxClassifiers();
    public abstract ClassifiedSpan FixClassification(SourceText text, ClassifiedSpan classifiedSpan);
    public abstract string? GetSyntacticClassificationForIdentifier(SyntaxToken identifier);

    public async Task AddSemanticClassificationsAsync(
        Document document,
        ImmutableArray<TextSpan> textSpans,
        ClassificationOptions options,
        Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
        Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
        SegmentedList<ClassifiedSpan> result,
        CancellationToken cancellationToken)
    {
        try
        {
            // No need to do nullable analysis for classification
            var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            AddSemanticClassifications(semanticModel, textSpans, getNodeClassifiers, getTokenClassifiers, result, options, cancellationToken);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }

    public void AddSemanticClassifications(
        SemanticModel semanticModel,
        ImmutableArray<TextSpan> textSpans,
        Func<SyntaxNode, ImmutableArray<ISyntaxClassifier>> getNodeClassifiers,
        Func<SyntaxToken, ImmutableArray<ISyntaxClassifier>> getTokenClassifiers,
        SegmentedList<ClassifiedSpan> result,
        ClassificationOptions options,
        CancellationToken cancellationToken)
    {
        Worker.Classify(semanticModel, textSpans, result, getNodeClassifiers, getTokenClassifiers, options, cancellationToken);
    }

    public TextChangeRange? ComputeSyntacticChangeRange(SyntaxNode oldRoot, SyntaxNode newRoot, TimeSpan timeout, CancellationToken cancellationToken)
        => SyntacticChangeRangeComputer.ComputeSyntacticChangeRange(oldRoot, newRoot, timeout, cancellationToken);
}
