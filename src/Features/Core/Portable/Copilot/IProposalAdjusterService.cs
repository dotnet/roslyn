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

    protected abstract Task<(bool changed, Document document)> AddMissingTokensIfAppropriateAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken);

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
        if (normalizedChanges.IsDefaultOrEmpty)
            return default;

        CopilotUtilities.ThrowIfNotNormalized(normalizedChanges);

        using var _ = ArrayBuilder<string>.GetInstance(out var adjustmentKinds);

        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked document, as that is what we will want to analyze.
        var oldText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var (newText, newSpans) = CopilotUtilities.GetNewTextAndChangedSpans(oldText, normalizedChanges);

        // Get the semantic model and keep it alive so none of the work we do causes it to be dropped.
        var forkedDocument = originalDocument.WithText(newText);
        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var format = false;
        var changed = false;

        // Apply the AddMissingTokens fix, and get a new document
        (changed, forkedDocument) = await AddMissingTokensIfAppropriateAsync(
                originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);
        if (changed)
        {
            adjustmentKinds.Add(ProposalAdjusterKinds.AddMissingTokens);
            format = true;
        }

        // Apply the AddMissingImports fix, and get a new document
        (changed, forkedDocument) = await TryGetAddImportTextChangesAsync(
            originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);
        if (changed)
        {
            adjustmentKinds.Add(ProposalAdjusterKinds.AddMissingImports);
            format = true;
        }

        // Apply the FormatCode fix, and get a new document
        (changed, forkedDocument) = await TryGetFormattingTextChangesAsync(
            originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);
        if (changed)
            adjustmentKinds.Add(ProposalAdjusterKinds.FormatCode);

        // If none of the adjustments were made, then just return what we were given.
        if (adjustmentKinds.IsEmpty)
            return new(normalizedChanges, format, default);

        // Keep the new root around, in case something needs it while processing.  This way we don't throw it away unnecessarily.
        GC.KeepAlive(forkedRoot);

        // Get the final set of changes between the original document and the new document.
        var allChanges = await forkedDocument.GetTextChangesAsync(originalDocument, cancellationToken).ConfigureAwait(false);
        var totalChanges = allChanges.AsImmutableOrEmpty();

        return new(totalChanges, format, adjustmentKinds.ToImmutableAndClear());
    }

    private async Task<(bool changed, Document forkedDocument)> TryGetAddImportTextChangesAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken)
    {
        if (!globalOptions.GetOption(CopilotOptions.FixAddMissingImports))
            return (false, forkedDocument);

        var missingImportsService = originalDocument.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        // Find the span of changes made to the forked document.
        var totalNewSpan = await GetSpanOfChangesAsync(originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);

        // Add the missing imports, but do not clean up the document.  We don't want the cleanup phase making edits that
        // may interfere with the changes copilot is making.  For example, in VB this may cause us to try to case correct
        // things.  This can conflict with the other edits, and also cause a more confusing experience for the user.  We
        // really just want to add the imports and let copilot handle anything else.
        var withImportsDocument = await missingImportsService.AddMissingImportsAsync(
            forkedDocument, totalNewSpan, cleanupDocument: false, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);

        return (true, withImportsDocument);
    }

    private async Task<(bool changed, Document document)> TryGetFormattingTextChangesAsync(
        Document originalDocument, Document forkedDocument, CancellationToken cancellationToken)
    {
        if (!globalOptions.GetOption(CopilotOptions.FixCodeFormat))
            return (false, forkedDocument);

        var syntaxFormattingService = originalDocument.GetRequiredLanguageService<ISyntaxFormattingService>();

        var formattingOptions = await originalDocument.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

        // Find the span of changes made to the forked document.
        var totalNewSpan = await GetSpanOfChangesAsync(originalDocument, forkedDocument, cancellationToken).ConfigureAwait(false);

        // Run the formatter on that span, and get the updated document.
        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var formatResult = syntaxFormattingService.GetFormattingResult(forkedRoot, [totalNewSpan], formattingOptions, rules: default, cancellationToken);
        var formattedRoot = formatResult.GetFormattedRoot(cancellationToken);
        var formattedDocument = forkedDocument.WithSyntaxRoot(formattedRoot);

        return (true, formattedDocument);
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
    /// <param name="oldDocument"></param>
    /// <param name="newDocument"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task<TextSpan> GetSpanOfChangesAsync(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
    {
        var forkedRoot = await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var changes = await newDocument.GetTextChangesAsync(oldDocument, cancellationToken).ConfigureAwait(false);
        var totalSpans = CopilotUtilities.GetTextSpansFromTextChanges(changes);
        var totalNewSpan = GetSpanToAnalyze(forkedRoot, totalSpans);
        return totalNewSpan;
    }

}
