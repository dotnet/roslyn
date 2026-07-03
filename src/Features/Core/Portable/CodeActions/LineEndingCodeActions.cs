// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeActions;

internal sealed class LineEndingDocumentChangeAction(
    string title,
    Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Document>> createChangedDocument,
    Document originalDocument,
    string? equivalenceKey) : CodeAction
{
    public override string Title => title;
    public override string? EquivalenceKey => equivalenceKey;

    internal override CodeActionCleanup Cleanup => CodeActionCleanup.None;

    protected override async Task<Document> GetChangedDocumentAsync(
        IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        var changedDocument = await createChangedDocument(progress, cancellationToken).ConfigureAwait(false);
        var cleanedDocument = await PostProcessChangesAsync(changedDocument, cancellationToken).ConfigureAwait(false);
        return await LineEndingUtilities.NormalizeLineEndingsAsync(
            cleanedDocument, originalDocument, fallbackLineEnding: null, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class LineEndingSolutionChangeAction(
    string title,
    Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> createChangedSolution,
    Solution originalSolution,
    string? equivalenceKey) : CodeAction
{
    public override string Title => title;
    public override string? EquivalenceKey => equivalenceKey;

    internal override CodeActionCleanup Cleanup => CodeActionCleanup.None;

    protected override async Task<Solution?> GetChangedSolutionAsync(
        IProgress<CodeAnalysisProgress> progress, CancellationToken cancellationToken)
    {
        var changedSolution = await createChangedSolution(progress, cancellationToken).ConfigureAwait(false);
        var cleanedSolution = await PostProcessChangesAsync(
            originalSolution, changedSolution, progress, CodeActionCleanup.Default, cancellationToken).ConfigureAwait(false);
        var documentIds = LineEndingUtilities.GetChangedDocumentIds(originalSolution, changedSolution)
            .Concat(LineEndingUtilities.GetChangedDocumentIds(originalSolution, cleanedSolution))
            .Distinct();

        return await LineEndingUtilities.NormalizeChangedDocumentsLineEndingsAsync(
            originalSolution, cleanedSolution, documentIds, cancellationToken).ConfigureAwait(false);
    }
}

internal static class LineEndingUtilities
{
    public static async Task<string> GetLineEndingAsync(
        Document document, string fallbackLineEnding, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        return GetLineEnding(text, fallbackLineEnding);
    }

    public static string GetLineEnding(SourceText text, string fallbackLineEnding)
    {
        foreach (var line in text.Lines)
        {
            if (line.EndIncludingLineBreak > line.End)
                return text.ToString(TextSpan.FromBounds(line.End, line.EndIncludingLineBreak));
        }

        return fallbackLineEnding;
    }

    public static async Task<Document> NormalizeLineEndingsAsync(
        Document changedDocument, Document originalDocument, string? fallbackLineEnding, CancellationToken cancellationToken)
    {
        var originalText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var lineEnding = GetLineEnding(originalText, fallbackLineEnding ?? Environment.NewLine);
        return await NormalizeLineEndingsAsync(changedDocument, lineEnding, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Document> NormalizeLineEndingsAsync(
        Document document, string lineEnding, CancellationToken cancellationToken)
    {
        var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var textString = text.ToString();
        var normalizedText = NormalizeLineEndings(textString, lineEnding);
        return normalizedText == textString
            ? document
            : document.WithText(SourceText.From(normalizedText, text.Encoding, text.ChecksumAlgorithm));
    }

    public static async Task<Solution> NormalizeChangedDocumentsLineEndingsAsync(
        Solution originalSolution, Solution changedSolution, CancellationToken cancellationToken)
        => await NormalizeChangedDocumentsLineEndingsAsync(
            originalSolution, changedSolution, GetChangedDocumentIds(originalSolution, changedSolution), cancellationToken).ConfigureAwait(false);

    public static IEnumerable<DocumentId> GetChangedDocumentIds(Solution originalSolution, Solution changedSolution)
    {
        var solutionChanges = changedSolution.GetChanges(originalSolution);
        return solutionChanges.GetProjectChanges()
            .SelectMany(p => p.GetChangedDocuments(onlyGetDocumentsWithTextChanges: true).Concat(p.GetAddedDocuments()))
            .Concat(solutionChanges.GetAddedProjects().SelectMany(p => p.DocumentIds))
            .Concat(solutionChanges.GetExplicitlyChangedSourceGeneratedDocuments());
    }

    public static async Task<Solution> NormalizeChangedDocumentsLineEndingsAsync(
        Solution originalSolution, Solution changedSolution, IEnumerable<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        foreach (var documentId in documentIds)
        {
            var changedDocument = await changedSolution.GetRequiredDocumentAsync(
                documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);

            if (!changedDocument.SupportsSyntaxTree)
                continue;

            var originalDocument = await originalSolution.GetDocumentAsync(
                documentId, includeSourceGenerated: true, cancellationToken).ConfigureAwait(false);
            var originalText = originalDocument is null
                ? null
                : await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var lineEnding = originalText is null ? Environment.NewLine : GetLineEnding(originalText, Environment.NewLine);
            changedDocument = await NormalizeLineEndingsAsync(changedDocument, lineEnding, cancellationToken).ConfigureAwait(false);
            changedSolution = changedDocument.Project.Solution;
        }

        return changedSolution;
    }

    private static string NormalizeLineEndings(string text, string lineEnding)
        => text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", lineEnding);
}
