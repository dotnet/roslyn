// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class RemoteDebuggingSessionProxy(SolutionServices services, IDisposable? connection, DebuggingSessionId sessionId) : IActiveStatementSpanFactory, IDisposable
{
    public void Dispose()
        => connection?.Dispose();

    private IEditAndContinueService GetLocalService()
        => services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

    public async ValueTask BreakStateOrCapabilitiesChangedAsync(bool? inBreakState, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            GetLocalService().BreakStateOrCapabilitiesChanged(sessionId, inBreakState);
        }
        else
        {
            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancallationToken) => service.BreakStateOrCapabilitiesChangedAsync(sessionId, inBreakState, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask EndDebuggingSessionAsync(CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            GetLocalService().EndDebuggingSession(sessionId);
        }
        else
        {
            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancallationToken) => service.EndDebuggingSessionAsync(sessionId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }

        Dispose();
    }

    public async ValueTask<(
            ModuleUpdates updates,
            ImmutableArray<DiagnosticData> diagnostics,
            ImmutableArray<DiagnosticData> rudeEdits,
            DiagnosticData? syntaxError)> EmitSolutionUpdateAsync(
        Solution solution,
        ActiveStatementSpanProvider activeStatementSpanProvider,
        CancellationToken cancellationToken)
    {
        ModuleUpdates moduleUpdates;
        ImmutableArray<DiagnosticData> diagnosticData;
        ImmutableArray<DiagnosticData> rudeEdits;
        DiagnosticData? syntaxError;

        try
        {
            var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var results = await GetLocalService().EmitSolutionUpdateAsync(sessionId, solution, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
                moduleUpdates = results.ModuleUpdates;
                diagnosticData = results.Diagnostics.ToDiagnosticData(solution);
                rudeEdits = results.RudeEdits.ToDiagnosticData(solution);
                syntaxError = results.GetSyntaxErrorData(solution);
            }
            else
            {
                var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, EmitSolutionUpdateResults.Data>(
                    solution,
                    (service, solutionInfo, callbackId, cancellationToken) => service.EmitSolutionUpdateAsync(solutionInfo, callbackId, sessionId, cancellationToken),
                    callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                    cancellationToken).ConfigureAwait(false);

                if (result.HasValue)
                {
                    moduleUpdates = result.Value.ModuleUpdates;
                    diagnosticData = result.Value.Diagnostics;
                    rudeEdits = result.Value.RudeEdits;
                    syntaxError = result.Value.SyntaxError;
                }
                else
                {
                    moduleUpdates = new ModuleUpdates(ModuleUpdateStatus.RestartRequired, []);
                    diagnosticData = [];
                    rudeEdits = [];
                    syntaxError = null;
                }
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            diagnosticData = GetInternalErrorDiagnosticData(solution, e);
            rudeEdits = [];
            moduleUpdates = new ModuleUpdates(ModuleUpdateStatus.RestartRequired, []);
            syntaxError = null;
        }

        return (moduleUpdates, diagnosticData, rudeEdits, syntaxError);
    }

    private static ImmutableArray<DiagnosticData> GetInternalErrorDiagnosticData(Solution solution, Exception e)
    {
        var descriptor = EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind.InternalError);

        var diagnostic = Diagnostic.Create(
            descriptor,
            Location.None,
            string.Format(descriptor.MessageFormat.ToString(), "", e.Message));

        return [DiagnosticData.Create(solution, diagnostic, project: null)];
    }

    public async ValueTask CommitSolutionUpdateAsync(CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            GetLocalService().CommitSolutionUpdate(sessionId);
        }
        else
        {
            await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                (service, cancallationToken) => service.CommitSolutionUpdateAsync(sessionId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async ValueTask DiscardSolutionUpdateAsync(CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            GetLocalService().DiscardSolutionUpdate(sessionId);
            return;
        }

        await client.TryInvokeAsync<IRemoteEditAndContinueService>(
            (service, cancellationToken) => service.DiscardSolutionUpdateAsync(sessionId, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<ImmutableArray<ImmutableArray<ActiveStatementSpan>>> GetBaseActiveStatementSpansAsync(Solution solution, ImmutableArray<DocumentId> documentIds, CancellationToken cancellationToken)
    {
        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            return await GetLocalService().GetBaseActiveStatementSpansAsync(sessionId, solution, documentIds, cancellationToken).ConfigureAwait(false);
        }

        var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ImmutableArray<ActiveStatementSpan>>>(
            solution,
            (service, solutionInfo, cancellationToken) => service.GetBaseActiveStatementSpansAsync(solutionInfo, sessionId, documentIds, cancellationToken),
            cancellationToken).ConfigureAwait(false);

        return result.HasValue ? result.Value : [];
    }

    public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetAdjustedActiveStatementSpansAsync(TextDocument document, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
    {
        // filter out documents that are not synchronized to remote process before we attempt remote invoke:
        if (!RemoteSupportedLanguages.IsSupported(document.Project.Language))
        {
            return [];
        }

        var client = await RemoteHostClient.TryGetClientAsync(services, cancellationToken).ConfigureAwait(false);
        if (client == null)
        {
            return await GetLocalService().GetAdjustedActiveStatementSpansAsync(sessionId, document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);
        }

        var result = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<ActiveStatementSpan>>(
            document.Project.Solution,
            (service, solutionInfo, callbackId, cancellationToken) => service.GetAdjustedActiveStatementSpansAsync(solutionInfo, callbackId, sessionId, document.Id, cancellationToken),
            callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
            cancellationToken).ConfigureAwait(false);

        return result.HasValue ? result.Value : [];
    }
}
