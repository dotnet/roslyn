﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Copilot;

// The entire AdjusterProvider api is marked as obsolete since this is a preview API.  So we do the same here as well.
[Export(typeof(ProposalAdjusterProviderBase))]
[Obsolete("This is a preview api and subject to change")]
[ContentType(ContentTypeNames.CSharpContentType)]
[ContentType(ContentTypeNames.VisualBasicContentType)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class RoslynProposalAdjusterProvider() : ProposalAdjusterProviderBase
{
    public override Task<ProposalBase> AdjustProposalBeforeDisplayAsync(ProposalBase proposal, string providerName, CancellationToken cancellationToken)
        => AdjustProposalAsync(proposal, providerName, before: true, cancellationToken);

    public override Task<ProposalBase> AdjustProposalAfterAcceptAsync(ProposalBase proposal, string providerName, CancellationToken cancellationToken)
        => AdjustProposalAsync(proposal, providerName, before: false, cancellationToken);

    private static void SetDefaultTelemetryProperties(Dictionary<string, object?> map, string providerName, bool before, TimeSpan elapsedTime)
    {
        // Common properties that all adjustments will log.
        map["ProviderName"] = providerName;
        map["AdjustProposalBeforeDisplay"] = before;
        map["ComputationTime"] = elapsedTime.TotalMilliseconds.ToString("G17", CultureInfo.InvariantCulture);
    }

    private async Task<ProposalBase> AdjustProposalAsync(
        ProposalBase proposal, string providerName, bool before, CancellationToken cancellationToken)
    {
        var stopwatch = SharedStopwatch.StartNew();

        // Ensure we're only operating on one solution.  It makes the logic much simpler, as we don't have to
        // worry about edits that touch multiple solutions.
        var (solution, failureReason) = CopilotEditorUtilities.TryGetAffectedSolution(proposal);
        if (solution is null)
        {
            // If we can't find a solution, then we can't adjust the proposal.  Log telemetry and return the original proposal.
            Logger.LogBlock(FunctionId.Copilot_AdjustProposal, KeyValueLogMessage.Create(static (d, args) =>
            {
                var (providerName, before, failureReason, elapsedTime) = args;
                SetDefaultTelemetryProperties(d, providerName, before, elapsedTime);
                d["SolutionAcquisitionFailure"] = failureReason;
            },
            args: (providerName, before, failureReason, stopwatch.Elapsed)),
            cancellationToken).Dispose();

            return proposal;
        }

        return await AdjustProposalAsync(
            solution, proposal, providerName, before, stopwatch, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProposalBase> AdjustProposalAsync(
        Solution solution,
        ProposalBase proposal,
        string providerName,
        bool before,
        SharedStopwatch stopwatch,
        CancellationToken cancellationToken)
    {
        try
        {
            var newProposal = await AdjustProposalAsync(
                solution, proposal, cancellationToken).ConfigureAwait(false);

            // Report telemetry if we were or were not able to adjust the proposal.
            Logger.LogBlock(FunctionId.Copilot_AdjustProposal, KeyValueLogMessage.Create(static (d, args) =>
            {
                var (providerName, before, proposal, newProposal, elapsedTime) = args;

                // If we (roslyn) were able to come up with *any* edits we wanted to adjust the proposal with or not.
                var adjustmentsProposed = newProposal != null;

                newProposal ??= proposal;

                // Whether or not the Proposal system accepted the edits we proposed.  It can reject them for a variety of reasons,
                // for example, if it thinks they would interfere with the caret, or if edits intersect other edits.
                var adjustmentsAccepted = newProposal != proposal;

                SetDefaultTelemetryProperties(d, providerName, before, elapsedTime);

                d["AdjustmentsProposed"] = adjustmentsProposed;
                d["AdjustmentsAccepted"] = adjustmentsAccepted;

                // Record how many new edits were made to the proposal.  Expectation is that this is commonly only 1,
                // but we want to see how that potentially changes over time, especially as we add more adjusters.
                d["AdjustmentsCount"] = newProposal.Edits.Count - proposal.Edits.Count;
            },
            args: (providerName, before, proposal, newProposal, stopwatch.Elapsed)),
            cancellationToken).Dispose();

            return newProposal ?? proposal;
        }
        catch (Exception ex) when (ReportFailureTelemetry(ex))
        {
            // Don't leak out any exceptions.  We report them as telemetry (and/or NFW), but we don't want to block the
            // user getting the original proposal.
            return proposal;
        }

        bool ReportFailureTelemetry(Exception ex)
        {
            // If it's not cancellation, report an NFW to track down our bug.
            if (ex is not OperationCanceledException)
                FatalError.ReportAndCatch(ex);

            Logger.LogBlock(FunctionId.Copilot_AdjustProposal, KeyValueLogMessage.Create(static (d, args) =>
            {
                var (providerName, before, ex, elapsedTime) = args;
                SetDefaultTelemetryProperties(d, providerName, before, elapsedTime);
                d["AdjustmentsProposed"] = false;

                if (ex is OperationCanceledException)
                {
                    d["Canceled"] = true;
                }
                else
                {
                    // Will be able to get a count of how often this happens.  NFWs can be used to track down the issue.
                    d["Failed"] = true;
                }
            },
            args: (providerName, before, ex, stopwatch.Elapsed)),
            cancellationToken).Dispose();

            return true;
        }
    }

    /// <summary>
    /// Returns <see langword="null"/> if we didn't make any adjustments to the proposal.  Otherwise, returns the attempted
    /// adjusted proposal based on the edits we tried to make.  Note that this does not guarantee that the edits were successfully
    /// applied to the original edits.  The <see cref="Proposal"/> system may reject them based on their own criteria.
    /// </summary>
    private async Task<ProposalBase?> AdjustProposalAsync(
        Solution solution, ProposalBase proposal, CancellationToken cancellationToken)
    {
        // We're potentially making multiple calls to oop here.  So keep a session alive to avoid
        // resyncing the solution and recomputing compilations.
        using var _1 = await RemoteKeepAliveSession.CreateAsync(solution, cancellationToken).ConfigureAwait(false);
        using var _2 = PooledObjects.ArrayBuilder<ProposedEdit>.GetInstance(out var finalEdits);

        var adjustmentsProposed = false;
        foreach (var editGroup in proposal.Edits.GroupBy(e => e.Span.Snapshot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var snapshot = editGroup.Key;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();

            // Checked in TryGetAffectedSolution
            Contract.ThrowIfNull(document);

            var proposalAdjusterService = document.Project.Solution.Services.GetRequiredService<ICopilotProposalAdjusterService>();
            var proposedEdits = await proposalAdjusterService.TryAdjustProposalAsync(
                document, CopilotEditorUtilities.TryGetNormalizedTextChanges(editGroup), cancellationToken).ConfigureAwait(false);

            if (proposedEdits.IsDefault)
            {
                // No changes were made to the proposal.  Just add the original edits.
                finalEdits.AddRange(editGroup);
            }
            else
            {
                // Changes were made to the proposal.  Add the new edits.
                adjustmentsProposed = true;
                foreach (var proposedEdit in proposedEdits)
                {
                    finalEdits.Add(new ProposedEdit(
                        new(snapshot, proposedEdit.Span.ToSpan()),
                        proposedEdit.NewText!));
                }
            }
        }

        // No adjustments were made.  Don't touch anything.
        if (!adjustmentsProposed)
            return null;

        // We have some changes we want to to make to the proposal.  See if the proposal system allows us merging
        // those changes in.  Note: we should generally always be producing edits that are safe to merge in.  However,
        // as we do not control this code, we cannot guarantee this.  Telemetry will let us know how often this happens
        // and if there's something we need to look into.
        return Proposal.TryCreateProposal(proposal, finalEdits);
    }
}
