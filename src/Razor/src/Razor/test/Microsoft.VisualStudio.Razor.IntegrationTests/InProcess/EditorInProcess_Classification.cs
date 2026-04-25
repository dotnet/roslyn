// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Xunit;

namespace Microsoft.VisualStudio.Extensibility.Testing;

internal partial class EditorInProcess
{
    /// <summary>
    /// Waits for the Razor component semantic classifications to be available on the active TextView
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="count">The number of the given classification to expect.</param>
    /// <param name="exact">Whether to wait for exactly <paramref name="count"/> classifications.</param>
    /// <returns>A <see cref="Task"/> which completes when classification is "ready".</returns>
    public Task WaitForComponentClassificationAsync(CancellationToken cancellationToken, int count = 1, bool exact = false)
        => WaitForSemanticClassificationAsync("RazorComponentElement", cancellationToken, count, exact);

    /// <summary>
    /// Waits for any semantic classifications to be available on the active TextView, and for at least one of the
    /// <paramref name="expectedClassification"/> if provided.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <param name="expectedClassification">The classification to wait for, if any.</param>
    /// <param name="count">The number of the given classification to expect.</param>
    /// <param name="exact">Whether to wait for exactly <paramref name="count"/> classifications.</param>
    /// <returns>A <see cref="Task"/> which completes when classification is "ready".</returns>
    public async Task WaitForSemanticClassificationAsync(string expectedClassification, CancellationToken cancellationToken, int count = 1, bool exact = false)
    {
        var textView = await TestServices.Editor.GetActiveTextViewAsync(cancellationToken);
        var classifier = await GetClassifierAsync(textView, cancellationToken);

        using var semaphore = new SemaphoreSlim(1);
        await semaphore.WaitAsync(cancellationToken);

        classifier.ClassificationChanged += Classifier_ClassificationChanged;

        // Check that we're not ALREADY changed
        if (HasClassification(classifier, textView, expectedClassification, count, exact))
        {
            semaphore.Release();
            classifier.ClassificationChanged -= Classifier_ClassificationChanged;
            return;
        }

        try
        {
            await semaphore.WaitAsync(cancellationToken);
        }
        finally
        {
            classifier.ClassificationChanged -= Classifier_ClassificationChanged;
        }

        void Classifier_ClassificationChanged(object sender, ClassificationChangedEventArgs e)
        {
            if (HasClassification(classifier, textView, expectedClassification, count, exact))
            {
                semaphore.Release();
            }
        }

        static bool HasClassification(IClassifier classifier, ITextView textView, string expectedClassification, int count, bool exact)
        {
            var classifications = GetClassifications(classifier, textView);

            var found = 0;
            foreach (var c in classifications)
            {
                if (ClassificationMatches(expectedClassification, c.ClassificationType) ||
                    c.ClassificationType.BaseTypes.Any(t => ClassificationMatches(expectedClassification, t)))
                {
                    found++;
                }
            }

            return found == count ||
                (!exact && found > count);
        }

        static bool ClassificationMatches(string expectedClassification, IClassificationType classificationType)
            => classificationType is ILayeredClassificationType layered &&
                layered.Layer == ClassificationLayer.Semantic &&
                layered.Classification == expectedClassification;
    }

    public async Task VerifyGetClassificationsAsync(IEnumerable<ClassificationSpan> expectedClassifications, CancellationToken cancellationToken)
    {
        var actualClassifications = await GetClassificationsAsync(cancellationToken);
        var actualArray = actualClassifications.ToArray();
        var expectedArray = expectedClassifications.ToArray();

        for (var i = 0; i < actualArray.Length; i++)
        {
            var actualClassification = actualArray[i];
            var expectedClassification = expectedArray[i];

            if (actualClassification.ClassificationType.BaseTypes.Count() > 1)
            {
                Assert.Equal(expectedClassification, actualClassification, ClassificationTypeComparer.Instance);
            }
            else if (!expectedClassification.Span.Span.Equals(actualClassification.Span.Span)
                || !string.Equals(expectedClassification.ClassificationType.Classification, actualClassification.ClassificationType.Classification))
            {
                Assert.Equal(expectedClassification.Span, actualClassification.Span);
                Assert.Equal(expectedClassification.ClassificationType.Classification, actualClassification.ClassificationType.Classification);

                Assert.Fail($"i: {i}" +
                    $"expected: {expectedClassification.Span} {expectedClassification.ClassificationType.Classification} " +
                    $"actual: {actualClassification.Span} {actualClassification.ClassificationType.Classification}");
            }
        }

        Assert.Equal(expectedArray.Length, actualArray.Length);
    }

    public async Task<IList<ClassificationSpan>> GetClassificationsAsync(CancellationToken cancellationToken)
    {
        var textView = await GetActiveTextViewAsync(cancellationToken);
        var classifier = await GetClassifierAsync(textView, cancellationToken);
        return GetClassifications(classifier, textView);
    }

    private static IList<ClassificationSpan> GetClassifications(IClassifier classifier, ITextView textView)
    {
        var selectionSpan = new SnapshotSpan(textView.TextSnapshot, new Span(0, textView.TextSnapshot.Length));

        return classifier.GetClassificationSpans(selectionSpan);
    }

    /// <summary>
    /// Validates that we aren't seeing disco colors in the editor
    /// </summary>
    /// <remarks>
    /// This actually just calls <see cref="GetClassificationsAsync(CancellationToken)"/> because we always check for disco colors
    /// when getting classifications, but this makes for a more discoverable API.
    /// </remarks>
    public async Task ValidateNoDiscoColorsAsync(CancellationToken cancellationToken)
    {
        var classifiedSpans = await GetClassificationsAsync(cancellationToken);

        ValidateNoDiscoColors(classifiedSpans);
    }

    private static void ValidateNoDiscoColors(IList<ClassificationSpan> classifiedSpans)
    {
        // We never expect a word to have a classification change in the middle of it, so we can check for disco colors
        // by making sure that each span either doesn't start with a letter or digit, or comes after something that isn't
        // a letter or digit.
        SnapshotSpan? previousSpan = null;
        foreach (var span in classifiedSpans)
        {
            if (span.Span.IsEmpty)
            {
                continue;
            }

            if (previousSpan is { } previous)
            {
                Assert.False(previous.End.Position == span.Span.Start.Position && char.IsLetterOrDigit(span.Span.Start.GetChar()) && char.IsLetterOrDigit((previous.End - 1).GetChar()), $"Disco colors detected: {previous.GetText()}{span.Span.GetText()} has classification {span.ClassificationType.Classification} starting at character {previous.Length}");
            }

            previousSpan = span.Span;
        }
    }

    private async Task<IClassifier> GetClassifierAsync(IWpfTextView textView, CancellationToken cancellationToken)
    {
        var classifierService = await GetComponentModelServiceAsync<IViewClassifierAggregatorService>(cancellationToken);

        return classifierService.GetClassifier(textView);
    }

    private class ClassificationTypeComparer : IEqualityComparer<ClassificationSpan>
    {
        public static ClassificationTypeComparer Instance { get; } = new();

        public bool Equals(ClassificationSpan x, ClassificationSpan y)
        {
            if (x.Span.Equals(y.Span))
            {
                var actualClassification = !x.ClassificationType.BaseTypes.Any() ? y : x;
                var expectedClassification = !x.ClassificationType.BaseTypes.Any() ? x : y;
                var semanticBaseTypes = actualClassification.ClassificationType.BaseTypes.Where(t => t is ILayeredClassificationType layered && layered.Layer == ClassificationLayer.Semantic);
                if (semanticBaseTypes.Count() == 1)
                {
                    return string.Equals(semanticBaseTypes.First().Classification, expectedClassification.ClassificationType.Classification);
                }
                else if (semanticBaseTypes.Count() > 1)
                {
                    return semanticBaseTypes.Select(s => s.Classification).Contains(expectedClassification.ClassificationType.Classification);
                }
                // Did not have semantic basetype
            }

            return false;
        }

        public int GetHashCode(ClassificationSpan obj)
        {
            throw new System.NotImplementedException();
        }
    }
}
