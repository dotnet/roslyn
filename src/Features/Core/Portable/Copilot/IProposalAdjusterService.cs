// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddMissingImports;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

internal interface ICopilotProposalAdjusterService : IWorkspaceService
{
    ValueTask<ImmutableArray<TextChange>> AdjustProposalAsync(
        Document document, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken);
}

internal interface IRemoteCopilotProposalAdjusterService
{
    ValueTask<ImmutableArray<TextChange>> AdjustProposalAsync(
        Checksum solutionChecksum, DocumentId documentId, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken);
}

[ExportWorkspaceService(typeof(ICopilotProposalAdjusterService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultCopilotProposalAdjusterService() : ICopilotProposalAdjusterService
{
    public async ValueTask<ImmutableArray<TextChange>> AdjustProposalAsync(
        Document document, ImmutableArray<TextChange> textChanges, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (client is not null)
        {
            var result = await client.TryInvokeAsync<IRemoteCopilotProposalAdjusterService, ImmutableArray<TextChange>>(
                document.Project,
                (service, checksum, cancellationToken) => service.AdjustProposalAsync(checksum, document.Id, textChanges, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            return result.HasValue ? result.Value : textChanges;
        }
        else
        {
            return await AdjustProposalInCurrentProcessAsync(
                document, textChanges, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<ImmutableArray<TextChange>> AdjustProposalInCurrentProcessAsync(
        Document originalDocument, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        var normalizedChanges = CopilotUtilities.NormalizeCopilotTextChanges(changes);
        if (normalizedChanges.IsDefaultOrEmpty)
            return changes;

        return await AdjustProposalInCurrentProcessWorkerAsync(originalDocument, normalizedChanges, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableArray<TextChange>> AdjustProposalInCurrentProcessWorkerAsync(
        Document originalDocument, ImmutableArray<TextChange> changes, CancellationToken cancellationToken)
    {
        // Fork the starting document with the changes copilot wants to make.  Keep track of where the edited spans
        // move to in the forked doucment, as that is what we will want to analyze.
        var oldText = await originalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

        using var _ = ArrayBuilder<TextSpan>.GetInstance(changes.Length, out var newSpans);
        var newText = CopilotChangeAnalysisUtilities.GetNewText(oldText, changes, newSpans);

        // Get the semantic model and keep it alive so none of the work we do causes it to be dropped.
        var forkedDocument = originalDocument.WithText(newText);
        var forkedRoot = await forkedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var totalNewSpan = GetSpanToAnalyze(forkedRoot, newSpans);

        var (success, addImportChanges) = await TryGetAddImportTextChangesAsync(
            originalDocument, forkedDocument, changes.First(), totalNewSpan, cancellationToken).ConfigureAwait(false);
        if (!success)
            return changes;

        // Keep the new root around, in case something needs it while processing.  This way we don't throw it away unnecessarily.
        GC.KeepAlive(forkedRoot);

        // Reurn the add-import changes concatenated with the original changes.  This way we ensure
        // that the copilot changes themselves are not themselves modified by the add-import changes.
        return addImportChanges.Concat(changes);
    }

    private static async Task<(bool success, ImmutableArray<TextChange> addImportChanges)> TryGetAddImportTextChangesAsync(
        Document originalDocument, Document forkedDocument, TextChange firstTextChange, TextSpan totalNewSpan, CancellationToken cancellationToken)
    {
        var missingImportsService = originalDocument.GetRequiredLanguageService<IAddMissingImportsFeatureService>();

        var withImportsDocument = await missingImportsService.AddMissingImportsAsync(
            forkedDocument, totalNewSpan, CodeAnalysisProgress.None, cancellationToken).ConfigureAwait(false);

        var allChanges = await withImportsDocument.GetTextChangesAsync(originalDocument, cancellationToken).ConfigureAwait(false);
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

        return (true, addImportChanges);
    }

    private static TextSpan GetSpanToAnalyze(SyntaxNode forkedRoot, ArrayBuilder<TextSpan> newSpans)
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
}
