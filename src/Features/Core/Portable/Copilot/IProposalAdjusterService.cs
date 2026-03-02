// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

using Adjuster = Func<Document, Document, CancellationToken, Task<Document>>;

internal static class ProposalAdjusterKinds
{
    public const string AddMissingImports = nameof(AddMissingImports);
    public const string AddMissingTokens = nameof(AddMissingTokens);
    public const string FormatCode = nameof(FormatCode);
}

[DataContract]
internal readonly record struct ProposalAdjustmentResult(
    [property: DataMember(Order = 0)] ImmutableArray<TextChange> TextChanges,
    [property: DataMember(Order = 1)] bool Format,
    [property: DataMember(Order = 2)] ImmutableArray<AdjustmentResult> AdjustmentResults);

[DataContract]
internal readonly record struct AdjustmentResult(
    [property: DataMember(Order = 0)] string AdjustmentKind,
    [property: DataMember(Order = 1)] TimeSpan AdjustmentTime);

internal interface ICopilotProposalAdjusterService : ILanguageService
{
    /// <returns><c>default</c> if the proposal was not adjusted</returns>
    ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments, Document document,
        ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken);
}

internal interface IRemoteCopilotProposalAdjusterService
{
    /// <inheritdoc cref="ICopilotProposalAdjusterService.TryAdjustProposalAsync"/>
    ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments, Checksum solutionChecksum,
        DocumentId documentId, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken);
}

internal abstract class AbstractCopilotProposalAdjusterService : ICopilotProposalAdjusterService
{
    protected readonly IGlobalOptionService globalOptions;

    private readonly ImmutableArray<(string name, Adjuster adjuster)> _adjusters;

    public AbstractCopilotProposalAdjusterService(IGlobalOptionService globalOptions)
    {
        this.globalOptions = globalOptions;
        _adjusters = [
            (ProposalAdjusterKinds.AddMissingTokens, this.AddMissingTokensIfAppropriateAsync),
            (ProposalAdjusterKinds.AddMissingImports, this.TryGetAddImportTextChangesAsync),
            (ProposalAdjusterKinds.FormatCode, this.TryGetFormattingTextChangesAsync),
        ];
    }

    protected abstract Task<Document> AddMissingTokensIfAppropriateAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken);

    public async ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments, Document document,
        ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken)
    {
        if (normalizedChanges.IsDefaultOrEmpty)
            return default;

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteCopilotProposalAdjusterService, ProposalAdjustmentResult>(
                document.Project,
                (service, checksum, cancellationToken) => service.TryAdjustProposalAsync(
                    allowableAdjustments, checksum, document.Id, normalizedChanges, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : default;
        }

        return await TryAdjustProposalInCurrentProcessAsync(
            allowableAdjustments, document, normalizedChanges, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProposalAdjustmentResult> TryAdjustProposalInCurrentProcessAsync(
        ImmutableHashSet<string> allowableAdjustments, Document originalDocument,
        ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken)
    {
        Debug.Assert(allowableAdjustments is not null);

        if (allowableAdjustments.IsEmpty)
            return new(normalizedChanges, Format: false, AdjustmentResults: default);

        if (normalizedChanges.IsDefaultOrEmpty)
            return default;

        CopilotUtilities.ThrowIfNotNormalized(normalizedChanges);

        using var _ = ArrayBuilder<AdjustmentResult>.GetInstance(out var adjustmentResults);

        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked document, as that is what we will want to analyze.
        var oldText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var (newText, newSpans) = CopilotUtilities.GetNewTextAndChangedSpans(oldText, normalizedChanges);

        // Get the semantic model and keep it alive so none of the work we do causes it to be dropped.
        var forkedDocument = originalDocument.WithText(newText);
        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (adjusterName, adjuster) in _adjusters)
        {
            if (allowableAdjustments is null || !allowableAdjustments.Contains(adjusterName))
                continue;

            var timer = SharedStopwatch.StartNew();
            var adjustedDocument = await adjuster(originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);
            if (forkedDocument != adjustedDocument)
            {
                adjustmentResults.Add(new(adjusterName, AdjustmentTime: timer.Elapsed));
                forkedDocument = adjustedDocument;
            }
        }

        // If none of the adjustments were made, then just return what we were given.
        if (adjustmentResults.IsEmpty)
            return new(normalizedChanges, Format: false, AdjustmentResults: default);

        // Keep the new root around, in case something needs it while processing.  This way we don't throw it away unnecessarily.
        GC.KeepAlive(forkedRoot);

        // Get the final set of changes between the original document and the new document.
        var allChanges = await forkedDocument.GetTextChangesAsync(originalDocument, cancellationToken).ConfigureAwait(false);
        var totalChanges = NormalizeLineEndingsInChanges(oldText, allChanges.AsImmutableOrEmpty());

        return new(totalChanges, Format: true, adjustmentResults.ToImmutableAndClear());
    }

    private async Task<Document> TryGetAddImportTextChangesAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken)
    {
        var missingImportsService = originalDocument.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        // Find the span of changes made to the forked document.
        var totalNewSpan = await GetSpanOfChangesAsync(originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);

        // Add the missing imports, but do not clean up the document.  We don't want the cleanup phase making edits that
        // may interfere with the changes copilot is making.  For example, in VB this may cause us to try to case correct
        // things.  This can conflict with the other edits, and also cause a more confusing experience for the user.  We
        // really just want to add the imports and let copilot handle anything else.
        var withImportsDocument = await missingImportsService.AddMissingImportsAsync(
            forkedDocument, totalNewSpan, cleanupDocument: false, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);

        return withImportsDocument;
    }

    private async Task<Document> TryGetFormattingTextChangesAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken)
    {
        var syntaxFormattingService = originalDocument.GetRequiredLanguageService<ISyntaxFormattingService>();

        var formattingOptions = await originalDocument.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Find the span of changes made to the forked document.
        var totalNewSpan = await GetSpanOfChangesAsync(originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);

        // Run the formatter on that span, and get the updated document.
        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var formatResult = syntaxFormattingService.GetFormattingResult(forkedRoot, [totalNewSpan], formattingOptions, rules: default, cancellationToken);
        var formattedRoot = formatResult.GetFormattedRoot(cancellationToken);
        var formattedDocument = forkedDocument.WithSyntaxRoot(formattedRoot);

        return formattedDocument;
    }

    private static TextSpan GetSpanToAnalyze(SyntaxNode forkedRoot, ImmutableArray<TextSpan> newSpans)
    {
        // Get the span that covers all the new spans that copilot wants to make.
        var newSpan = TextSpan.FromBounds(
            newSpans.Min(span => span.Start),
            newSpans.Max(span => span.End));

        // Now, if those spans intersect tokens, increase the range to include those tokens as well.
        var startToken = forkedRoot.FindToken(newSpan.Start);
        var endToken = forkedRoot.FindToken(newSpan.End);
        return TextSpan.FromBounds(
            startToken.FullSpan.Start,
            endToken.FullSpan.End);
    }

    /// <summary>
    /// Get a TextSpan that covers all the changes between the old and new document.
    /// </summary>
    private static async Task<TextSpan> GetSpanOfChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
    {
        var forkedRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
        var totalSpans = CopilotUtilities.GetTextSpansFromTextChanges(changes);
        var totalNewSpan = GetSpanToAnalyze(forkedRoot, totalSpans);
        return totalNewSpan;
    }

    /// <summary>
    /// Ensures that no <see cref="TextChange"/> modifies line break characters.  The Proposal system's
    /// rejects edits that modify a line break (e.g. adding a \r to a \n), and the suggestion
    /// service already corrects line break mismatches on insertion, so changing them here is both invalid
    /// and unnecessary. For each change, we match every line ending in <see cref="TextChange.NewText"/>
    /// 1:1 with the line endings found in the original text within the change's <see cref="TextChange.Span"/>.
    /// </summary>
    private static ImmutableArray<TextChange> NormalizeLineEndingsInChanges(
        SourceText originalText, ImmutableArray<TextChange> changes)
    {
        if (changes.IsDefaultOrEmpty)
            return changes;

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var result);
        foreach (var change in changes)
        {
            var newText = NormalizeNewlines(change.NewText ?? "", originalText, change.Span);
            result.Add(new TextChange(change.Span, newText));
        }

        return result.ToImmutableAndClear();
    }

    /// <summary>
    /// Normalizes every line ending in <paramref name="newText"/> to match the line endings found within
    /// <paramref name="span"/> of <paramref name="originalText"/>. Line endings are matched in order:
    /// the first line break in <paramref name="newText"/> gets the first line ending from the original
    /// span, the second gets the second, and so on. If <paramref name="newText"/> has more line breaks
    /// than the original span, the extras use the last line ending found in the span (or, if the span
    /// contains no line breaks, the nearest line ending from the surrounding text).
    /// </summary>
    private static string NormalizeNewlines(string newText, SourceText originalText, TextSpan span)
    {
        if (newText.IndexOf('\r') < 0 && newText.IndexOf('\n') < 0)
            return newText;

        // Collect the line endings within the original span, in order.
        using var _1 = ArrayBuilder<string>.GetInstance(out var originalEndings);
        GetLineEndingsInSpan(originalText, span, originalEndings);

        // Determine the fallback line ending to use for extra newlines in newText (or when
        // the original span had no line breaks at all, e.g. a pure insertion).
        var fallback = originalEndings.Count > 0
            ? originalEndings[^1]
            : GetLineEndingAtPosition(originalText, span.Start);

        // Walk through newText, replacing each line ending with the corresponding original one.
        using var _2 = PooledStringBuilder.GetInstance(out var sb);
        var endingIndex = 0;
        for (var i = 0; i < newText.Length; i++)
        {
            var ch = newText[i];
            if (ch == '\r' && i + 1 < newText.Length && newText[i + 1] == '\n')
            {
                sb.Append(endingIndex < originalEndings.Count ? originalEndings[endingIndex] : fallback);
                endingIndex++;
                i++;
            }
            else if (ch == '\r' || ch == '\n')
            {
                sb.Append(endingIndex < originalEndings.Count ? originalEndings[endingIndex] : fallback);
                endingIndex++;
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Collects all line endings whose start position (<see cref="TextLine.End"/>) falls within
    /// <paramref name="span"/> of <paramref name="text"/>, in document order.
    /// </summary>
    private static void GetLineEndingsInSpan(SourceText text, TextSpan span, ArrayBuilder<string> endings)
    {
        var startLine = text.Lines.GetLineFromPosition(span.Start).LineNumber;
        var endLine = text.Lines.GetLineFromPosition(span.End).LineNumber;

        for (var i = startLine; i <= endLine; i++)
        {
            var line = text.Lines[i];

            // Only include line endings that start within the span.
            if (line.End < span.Start || line.End >= span.End)
                continue;

            var breakLength = line.EndIncludingLineBreak - line.End;
            if (breakLength == 2)
                endings.Add("\r\n");
            else if (breakLength == 1)
                endings.Add(text[line.End] == '\n' ? "\n" : "\r");
        }
    }

    /// <summary>
    /// Gets the line ending used by the line at <paramref name="position"/> in <paramref name="text"/>.
    /// If that line has no line break (e.g. last line of the file), searches backwards for the nearest
    /// line that does. Falls back to <c>"\r\n"</c> if the file has no line endings at all.
    /// </summary>
    private static string GetLineEndingAtPosition(SourceText text, int position)
    {
        var lineIndex = text.Lines.GetLineFromPosition(position).LineNumber;

        // Walk backwards from the current line to find one with a line break.
        for (var i = lineIndex; i >= 0; i--)
        {
            var line = text.Lines[i];
            var breakLength = line.EndIncludingLineBreak - line.End;
            if (breakLength == 2)
                return "\r\n";
            if (breakLength == 1)
                return text[line.End] == '\n' ? "\n" : "\r";
        }

        // No line endings found anywhere before this position; try lines after.
        for (var i = lineIndex + 1; i < text.Lines.Count; i++)
        {
            var line = text.Lines[i];
            var breakLength = line.EndIncludingLineBreak - line.End;
            if (breakLength == 2)
                return "\r\n";
            if (breakLength == 1)
                return text[line.End] == '\n' ? "\n" : "\r";
        }

        // File has no line endings at all (single-line file).
        return "\r\n";
    }
}
