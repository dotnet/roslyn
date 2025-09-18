// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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

namespace Microsoft.CodeAnalysis.Copilot;

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
    [property: DataMember(Order = 2)] ImmutableArray<string> AdjustmentKinds);

internal interface ICopilotProposalAdjusterService : ILanguageService
{
    /// <returns><c>default</c> if the proposal was not adjusted</returns>
    ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        Document document, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken);
}

internal interface IRemoteCopilotProposalAdjusterService
{
    /// <inheritdoc cref="ICopilotProposalAdjusterService.TryAdjustProposalAsync"/>
    ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken);
}

internal abstract class AbstractCopilotProposalAdjusterService : ICopilotProposalAdjusterService
{
    protected readonly IGlobalOptionService globalOptions;

    protected AbstractCopilotProposalAdjusterService(IGlobalOptionService globalOptions)
    {
        this.globalOptions = globalOptions;
    }

    protected abstract Task<ImmutableArray<TextChange>> AddMissingTokensIfAppropriateAsync(
        Document originalDocument, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken);

    public async ValueTask<ProposalAdjustmentResult> TryAdjustProposalAsync(
        Document document, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken)
    {
        if (normalizedChanges.IsDefaultOrEmpty)
            return default;

        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteCopilotProposalAdjusterService, ProposalAdjustmentResult>(
                document.Project,
                (service, checksum, cancellationToken) => service.TryAdjustProposalAsync(checksum, document.Id, normalizedChanges, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : default;
        }
        else
        {
            return await TryAdjustProposalInCurrentProcessAsync(
                document, normalizedChanges, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<ProposalAdjustmentResult> TryAdjustProposalInCurrentProcessAsync(
        Document originalDocument, ImmutableArray<TextChange> normalizedChanges, CancellationToken cancellationToken)
    {
        CopilotUtilities.ThrowIfNotNormalized(normalizedChanges);

        using var _ = ArrayBuilder<string>.GetInstance(out var adjustmentKinds);

        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked document, as that is what we will want to analyze.
        var oldText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        var changesWhenMissingTokens = await AddMissingTokensIfAppropriateAsync(
                originalDocument, normalizedChanges, cancellationToken).ConfigureAwait(false);

        var format = false;
        if (!changesWhenMissingTokens.IsDefaultOrEmpty)
        {
            adjustmentKinds.Add(ProposalAdjusterKinds.AddMissingTokens);
            normalizedChanges = changesWhenMissingTokens;
            format = true;
        }

        var (newText, newSpans) = CopilotUtilities.GetNewTextAndChangedSpans(oldText, normalizedChanges);

        // Get the semantic model and keep it alive so none of the work we do causes it to be dropped.
        var forkedDocument = originalDocument.WithText(newText);
        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var totalNewSpan = GetSpanToAnalyze(forkedRoot, newSpans);

        var addImportChanges = await TryGetAddImportTextChangesAsync(
            originalDocument, forkedDocument, normalizedChanges.First(), totalNewSpan, cancellationToken).ConfigureAwait(false);
        if (!addImportChanges.IsDefaultOrEmpty)
        {
            adjustmentKinds.Add(ProposalAdjusterKinds.AddMissingImports);
            format = true;
        }

        var afterFormatChanges = await TryGetFormattingTextChangesAsync(
            originalDocument, forkedDocument, totalNewSpan, cancellationToken).ConfigureAwait(false);
        if (!afterFormatChanges.IsDefaultOrEmpty)
            adjustmentKinds.Add(ProposalAdjusterKinds.FormatCode);

        if (adjustmentKinds.IsEmpty)
            return new(normalizedChanges, format, default);

        // Keep the new root around, in case something needs it while processing.  This way we don't throw it away unnecessarily.
        GC.KeepAlive(forkedRoot);

        // Return the add-import changes concatenated with the original changes.  This way we ensure
        // that the copilot changes themselves are not themselves modified by the add-import changes.
        var beforeChanges = addImportChanges.IsDefault ? ImmutableArray<TextChange>.Empty : addImportChanges;
        var afterChanges = afterFormatChanges.IsDefault ? normalizedChanges : afterFormatChanges;

        var totalChanges = beforeChanges.Concat(afterChanges);
        return new(totalChanges, format, adjustmentKinds.ToImmutableAndClear());
    }

    private async Task<ImmutableArray<TextChange>> TryGetAddImportTextChangesAsync(
        Document originalDocument, Document forkedDocument, TextChange firstTextChange, TextSpan totalNewSpan, CancellationToken cancellationToken)
    {
        if (!globalOptions.GetOption(CopilotOptions.FixAddMissingImports))
            return default;

        var missingImportsService = originalDocument.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        // Add the missing imports, but do not clean up the document.  We don't want the cleanup phase making edits that
        // may interfere with the changes copilot is making.  For example, in VB this may cause us to try to case correct
        // things.  This can conflict with the other edits, and also cause a more confusing experience for the user.  We
        // really just want to add the imports and let copilot handle anything else.
        var withImportsDocument = await missingImportsService.AddMissingImportsAsync(
            forkedDocument, totalNewSpan, cleanupDocument: false, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);

        var allChanges = await withImportsDocument.GetTextChangesAsync(forkedDocument, cancellationToken).ConfigureAwait(false);
        var addImportChanges = allChanges.AsImmutableOrEmpty();

        // If there are no add-import changes, then we can just return the original changes.
        if (addImportChanges.IsEmpty)
            return default;

        // We only want to use the add-import changes if they're all before the earliest text change.
        // Otherwise, we might have a situation where the add-import changes intersect the copilot
        // changes and all bets are off.
        //
        // Note, as the text changes are normalized, we can assume that the first text change is the earliest one.
        if (!addImportChanges.All(textChange => textChange.Span.End < firstTextChange.Span.Start))
            return default;

        return addImportChanges;
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

    private async Task<ImmutableArray<TextChange>> TryGetFormattingTextChangesAsync(
        Document originalDocument, Document forkedDocument, TextSpan totalNewSpan, CancellationToken cancellationToken)
    {
        if (!globalOptions.GetOption(CopilotOptions.FixCodeFormat))
            return default;

        var syntaxFormattingService = originalDocument.GetRequiredLanguageService<ISyntaxFormattingService>();

        var formattingOptions = await originalDocument.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var formatResult = syntaxFormattingService.GetFormattingResult(forkedRoot, [totalNewSpan], formattingOptions, rules: default, cancellationToken);

        var formattedRoot = formatResult.GetFormattedRoot(cancellationToken);
        var formattedDocument = forkedDocument.WithSyntaxRoot(formattedRoot);

        var mergedChanges = await formattedDocument.GetTextChangesAsync(originalDocument, cancellationToken).ConfigureAwait(false);
        var afterFormatChanges = mergedChanges.AsImmutableOrEmpty();

        if (afterFormatChanges.IsEmpty)
            return default;

        return afterFormatChanges;
    }
}
