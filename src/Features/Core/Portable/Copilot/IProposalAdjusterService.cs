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

using Adjuster = Func<Document, Document, LineFormattingOptions?, CancellationToken, Task<Document>>;

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
    /// <param name="applicableToSpan">
    /// Indicates the span of the <c>CompletionState.ApplicableToSpan</c> on the original document.
    /// Edits that intersect this span will be split so they do not overlap it, since the proposal system
    /// requires that no edit intersect this span (except a zero-length edit at its end).
    /// </param>
    /// <returns><c>default</c> if the proposal was not adjusted</returns>
    ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments, Document document,
        ImmutableArray<TextChange> normalizedChanges, LineFormattingOptions? lineFormattingOptions,
        TextSpan? applicableToSpan, CancellationToken cancellationToken);
}

internal interface IRemoteCopilotProposalAdjusterService
{
    /// <inheritdoc cref="ICopilotProposalAdjusterService.TryAdjustProposalAsync"/>
    ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments, Checksum solutionChecksum,
        DocumentId documentId, ImmutableArray<TextChange> normalizedChanges,
        LineFormattingOptions? lineFormattingOptions, TextSpan? applicableToSpan,
        CancellationToken cancellationToken);
}

internal abstract class AbstractCopilotProposalAdjusterService : ICopilotProposalAdjusterService
{
    protected readonly IGlobalOptionService globalOptions;

    private readonly ImmutableArray<(string name, Adjuster adjuster)> _adjusters;

    public AbstractCopilotProposalAdjusterService(IGlobalOptionService globalOptions)
    {
        this.globalOptions = globalOptions;
        _adjusters = [
            (ProposalAdjusterKinds.AddMissingTokens, (original, forked, _, ct) => this.AddMissingTokensIfAppropriateAsync(original, forked, ct)),
            (ProposalAdjusterKinds.AddMissingImports, static (original, forked, _, ct) => TryGetAddImportTextChangesAsync(original, forked, ct)),
            (ProposalAdjusterKinds.FormatCode, static (original, forked, lineFormatting, ct) => TryGetFormattingTextChangesAsync(original, forked, lineFormatting, ct)),
        ];
    }

    protected abstract Task<Document> AddMissingTokensIfAppropriateAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken);

    public async ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        ImmutableHashSet<string> allowableAdjustments, Document document,
        ImmutableArray<TextChange> normalizedChanges, LineFormattingOptions? lineFormattingOptions,
        TextSpan? applicableToSpan, CancellationToken cancellationToken)
    {
        if (normalizedChanges.IsDefaultOrEmpty)
            return default;

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteCopilotProposalAdjusterService, ProposalAdjustmentResult>(
                document.Project,
                (service, checksum, cancellationToken) => service.TryAdjustProposalAsync(
                    allowableAdjustments, checksum, document.Id, normalizedChanges,
                    lineFormattingOptions, applicableToSpan, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result.HasValue ? result.Value : default;
        }

        return await TryAdjustProposalInCurrentProcessAsync(
            allowableAdjustments, document, normalizedChanges, lineFormattingOptions,
            applicableToSpan, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProposalAdjustmentResult> TryAdjustProposalInCurrentProcessAsync(
        ImmutableHashSet<string> allowableAdjustments, Document originalDocument,
        ImmutableArray<TextChange> normalizedChanges, LineFormattingOptions? lineFormattingOptions,
        TextSpan? applicableToSpan, CancellationToken cancellationToken)
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
            var adjustedDocument = await adjuster(originalDocument, forkedDocument, lineFormattingOptions, cancellationToken).ConfigureAwait(false);
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
        var totalChanges = FixLineEndingBoundaries(oldText, allChanges.AsImmutableOrEmpty());

        // Merge nearby changes into a single TextChange that spans across the ATS boundary. Split any such changes.
        if (applicableToSpan is { } ats)
        {
            totalChanges = ConstrainChangesToAvoidSpan(oldText, totalChanges, ats);
            if (totalChanges.IsDefault)
                return new(normalizedChanges, Format: false, AdjustmentResults: default);
        }

        return new(totalChanges, Format: true, adjustmentResults.ToImmutableAndClear());
    }

    /// <summary>
    /// If replacement text starts with \n adjacent to \r, or ends with \r adjacent to
    /// \n, strip the offending character and shrink the span when the original text at the boundary
    /// matches the dropped character.
    /// </summary>
    private static ImmutableArray<TextChange> FixLineEndingBoundaries(
        SourceText originalText, ImmutableArray<TextChange> changes)
    {
        if (changes.IsDefaultOrEmpty)
            return changes;

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var result);
        var anyFixed = false;

        foreach (var change in changes)
        {
            var span = change.Span;
            var newText = change.NewText ?? "";
            var changed = false;

            // Use a loop to handle cases where stripping one boundary character reveals
            // another one (e.g., newText ends with "\r\r" adjacent to '\n').
            while (newText.Length > 0)
            {
                var fixedThisIteration = false;

                if (newText[0] == '\n' &&
                    span.Start > 0 &&
                    originalText[span.Start - 1] == '\r')
                {
                    // The replacement text would add a \n to a \r, changing the nature of the line break.
                    if (span.Start < originalText.Length && originalText[span.Start] == '\n')
                    {
                        // The \n exists in the original text. There is no reason to replace it.
                        span = TextSpan.FromBounds(span.Start + 1, Math.Max(span.Start + 1, span.End));
                    }

                    newText = newText[1..];
                    fixedThisIteration = true;
                }

                if (newText.Length > 0 && newText[^1] == '\r' &&
                    span.End < originalText.Length &&
                    originalText[span.End] == '\n')
                {
                    // The replacement text would add a \r to a \n, changing the nature of the line break.
                    if (span.End > 0 && originalText[span.End - 1] == '\r')
                    {
                        // The \r already exists in the original text. There is no reason to replace it.
                        span = TextSpan.FromBounds(Math.Min(span.Start, span.End - 1), span.End - 1);
                    }

                    newText = newText[..^1];
                    fixedThisIteration = true;
                }

                changed = changed || fixedThisIteration;

                if (!fixedThisIteration)
                    break;
            }

            anyFixed = anyFixed || changed;
            result.Add(changed ? new TextChange(span, newText) : change);
        }

        return anyFixed ? result.ToImmutableAndClear() : changes;
    }

    /// <summary>
    /// The proposal system requires that no edit intersect the ApplicableToSpan (except a zero-length edit at its end).
    /// If the diff algorithm merged adjacent changes into a single <see cref="TextChange"/> that spans across the ATS boundary,
    /// split it into before-ATS and after-ATS parts so the proposal system accepts the edits.
    /// </summary>
    /// <returns>
    /// The constrained changes, or <c>default</c> if splitting was not possible (caller should fall back to
    /// the original unadjusted changes).
    /// </returns>
    internal static ImmutableArray<TextChange> ConstrainChangesToAvoidSpan(
        SourceText originalText, ImmutableArray<TextChange> changes, TextSpan protectedSpan)
    {
        if (changes.IsDefaultOrEmpty || protectedSpan.IsEmpty)
            return changes;

        using var _ = ArrayBuilder<TextChange>.GetInstance(out var result);
        var anyConstrained = false;

        foreach (var change in changes)
        {
            if (!IntersectsProtectedSpan(change.Span, protectedSpan))
            {
                result.Add(change);
                continue;
            }

            anyConstrained = true;

            // The only case we can handle safely is when the change fully contains the
            // protected span, because we can find the protected text in newText and split.
            if (change.Span.Start > protectedSpan.Start || change.Span.End < protectedSpan.End)
            {
                // Partial overlap or change is contained within the protected span.
                // We can't safely split this. Bail out.
                return default;
            }

            if (!TrySplitChangeAroundProtectedSpan(originalText, change, protectedSpan, result))
                return default;
        }

        return anyConstrained ? result.ToImmutableAndClear() : changes;
    }

    /// <summary>
    /// Determines whether <paramref name="editSpan"/> intersects <paramref name="protectedSpan"/>.
    /// A zero-length edit at the end of the protected span is allowed (per <c>Proposal.ValidateEdits</c>)
    /// and is NOT considered an intersection.
    /// </summary>
    private static bool IntersectsProtectedSpan(TextSpan editSpan, TextSpan protectedSpan)
    {
        // A zero-length edit at the end of the protected span is allowed.
        if (editSpan.Length == 0 && editSpan.Start == protectedSpan.End)
            return false;

        // Two spans intersect if they share at least one position.
        return editSpan.Start < protectedSpan.End && editSpan.End > protectedSpan.Start;
    }

    /// <summary>
    /// Splits a <see cref="TextChange"/> that fully contains the protected span into parts that avoid it.
    /// Finds the protected text within <paramref name="change"/>.<see cref="TextChange.NewText"/> and splits
    /// around it, discarding the portion that corresponds to the protected span.
    /// </summary>
    private static bool TrySplitChangeAroundProtectedSpan(
        SourceText originalText,
        TextChange change,
        TextSpan protectedSpan,
        ArrayBuilder<TextChange> result)
    {
        Debug.Assert(change.Span.Start <= protectedSpan.Start && change.Span.End >= protectedSpan.End);

        var newText = change.NewText ?? "";
        var protectedText = originalText.ToString(protectedSpan);

        if (protectedText.Length == 0)
            return false;

        // Find the protected text in newText. Adjusters (formatting, missing tokens, imports)
        // don't modify identifiers, so the ATS text should be preserved in the replacement.
        var protectedIndex = FindProtectedTextInNewText(originalText, change.Span, newText, protectedSpan, protectedText);
        if (protectedIndex < 0)
            return false;

        // Verify the match is actually the protected text.
        if (protectedIndex + protectedText.Length > newText.Length ||
            string.Compare(newText, protectedIndex, protectedText, 0, protectedText.Length, StringComparison.Ordinal) != 0)
        {
            return false;
        }

        // Emit the portion before the protected span.
        var beforeSpan = TextSpan.FromBounds(change.Span.Start, protectedSpan.Start);
        var beforeText = newText[..protectedIndex];

        if (beforeSpan.Length > 0 || beforeText.Length > 0)
            result.Add(new TextChange(beforeSpan, beforeText));

        // Emit the portion after the protected span.
        var afterSpan = TextSpan.FromBounds(protectedSpan.End, change.Span.End);
        var afterText = newText[(protectedIndex + protectedText.Length)..];

        if (afterSpan.Length > 0 || afterText.Length > 0)
            result.Add(new TextChange(afterSpan, afterText));

        return true;
    }

    /// <summary>
    /// Finds the position of the protected (ATS) text within a change's replacement text.
    /// Uses surrounding context characters from the original text to disambiguate when the ATS
    /// text is short and could appear in multiple places.
    /// </summary>
    private static int FindProtectedTextInNewText(
        SourceText originalText,
        TextSpan changeSpan,
        string newText,
        TextSpan protectedSpan,
        string protectedText)
    {
        // Use the character immediately before the ATS in the original text for disambiguation.
        // This character (e.g., '.' in "Console.wl") is a non-whitespace token boundary that
        // formatting adjusters preserve.
        if (protectedSpan.Start > changeSpan.Start)
        {
            var charBefore = originalText[protectedSpan.Start - 1];
            var searchText = charBefore + protectedText;
            var idx = newText.IndexOf(searchText, StringComparison.Ordinal);
            if (idx >= 0)
                return idx + 1;
        }

        // Fallback: plain search for the protected text.
        return newText.IndexOf(protectedText, StringComparison.Ordinal);
    }

    private static async Task<Document> TryGetAddImportTextChangesAsync(
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

    private static async Task<Document> TryGetFormattingTextChangesAsync(
        Document originalDocument, Document forkedDocument,
        LineFormattingOptions? lineFormattingOptions, CancellationToken cancellationToken)
    {
        var syntaxFormattingService = originalDocument.GetRequiredLanguageService<ISyntaxFormattingService>();

        var formattingOptions = await originalDocument.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Override with the buffer-derived line formatting options if available, so the formatter
        // uses the file's actual newline character and inferred indentation settings.
        if (lineFormattingOptions is not null)
            formattingOptions = formattingOptions with { LineFormatting = lineFormattingOptions };

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

    internal readonly struct TestAccessor
    {
        internal static ImmutableArray<TextChange> FixLineEndingBoundaries(
            SourceText originalText, ImmutableArray<TextChange> changes)
            => AbstractCopilotProposalAdjusterService.FixLineEndingBoundaries(originalText, changes);

        internal static ImmutableArray<TextChange> ConstrainChangesToAvoidSpan(
            SourceText originalText, ImmutableArray<TextChange> changes, TextSpan protectedSpan)
            => AbstractCopilotProposalAdjusterService.ConstrainChangesToAvoidSpan(originalText, changes, protectedSpan);
    }
}
