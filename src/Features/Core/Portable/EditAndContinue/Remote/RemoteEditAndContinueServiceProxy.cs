﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Facade used to call remote <see cref="IRemoteEditAndContinueService"/> methods.
    /// Encapsulates all RPC logic as well as dispatching to the local service if the remote service is disabled.
    /// THe facade is useful for targeted testing of serialization/deserialization of EnC service calls.
    /// </summary>
    internal readonly partial struct RemoteEditAndContinueServiceProxy(Workspace workspace)
    {
        [ExportRemoteServiceCallbackDispatcher(typeof(IRemoteEditAndContinueService)), Shared]
        internal sealed class CallbackDispatcher : RemoteServiceCallbackDispatcher, IRemoteEditAndContinueService.ICallback
        {
            [ImportingConstructor]
            [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
            public CallbackDispatcher()
            {
            }

            public ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(RemoteServiceCallbackId callbackId, DocumentId? documentId, string filePath, CancellationToken cancellationToken)
                => ((ActiveStatementSpanProviderCallback)GetCallback(callbackId)).GetSpansAsync(documentId, filePath, cancellationToken);

            public ValueTask<string?> TryGetMatchingSourceTextAsync(RemoteServiceCallbackId callbackId, string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
                => ((DebuggingSessionCallback)GetCallback(callbackId)).TryGetMatchingSourceTextAsync(filePath, requiredChecksum, checksumAlgorithm, cancellationToken);

            public ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => ((DebuggingSessionCallback)GetCallback(callbackId)).GetActiveStatementsAsync(cancellationToken);

            public ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
                => ((DebuggingSessionCallback)GetCallback(callbackId)).GetAvailabilityAsync(mvid, cancellationToken);

            public ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(RemoteServiceCallbackId callbackId, CancellationToken cancellationToken)
                => ((DebuggingSessionCallback)GetCallback(callbackId)).GetCapabilitiesAsync(cancellationToken);

            public ValueTask PrepareModuleForUpdateAsync(RemoteServiceCallbackId callbackId, Guid mvid, CancellationToken cancellationToken)
                => ((DebuggingSessionCallback)GetCallback(callbackId)).PrepareModuleForUpdateAsync(mvid, cancellationToken);
        }

        private sealed class DebuggingSessionCallback(IManagedHotReloadService debuggerService, IPdbMatchingSourceTextProvider sourceTextProvider)
        {
            private readonly IManagedHotReloadService _debuggerService = debuggerService;
            private readonly IPdbMatchingSourceTextProvider _sourceTextProvider = sourceTextProvider;

            public async ValueTask<string?> TryGetMatchingSourceTextAsync(string filePath, ImmutableArray<byte> requiredChecksum, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
            {
                try
                {
                    return await _sourceTextProvider.TryGetMatchingSourceTextAsync(filePath, requiredChecksum, checksumAlgorithm, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return null;
                }
            }

            public async ValueTask<ImmutableArray<ManagedActiveStatementDebugInfo>> GetActiveStatementsAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _debuggerService.GetActiveStatementsAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return ImmutableArray<ManagedActiveStatementDebugInfo>.Empty;
                }
            }

            public async ValueTask<ManagedHotReloadAvailability> GetAvailabilityAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    return await _debuggerService.GetAvailabilityAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return new ManagedHotReloadAvailability(ManagedHotReloadAvailabilityStatus.InternalError, e.Message);
                }
            }

            public async ValueTask PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken)
            {
                try
                {
                    await _debuggerService.PrepareModuleForUpdateAsync(mvid, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    // nop
                }
            }

            public async ValueTask<ImmutableArray<string>> GetCapabilitiesAsync(CancellationToken cancellationToken)
            {
                try
                {
                    return await _debuggerService.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                {
                    return ImmutableArray<string>.Empty;
                }
            }
        }

        public readonly Workspace Workspace = workspace;

        private IEditAndContinueService GetLocalService()
            => Workspace.Services.GetRequiredService<IEditAndContinueWorkspaceService>().Service;

        public async ValueTask<RemoteDebuggingSessionProxy?> StartDebuggingSessionAsync(
            Solution solution,
            IManagedHotReloadService debuggerService,
            IPdbMatchingSourceTextProvider sourceTextProvider,
            ImmutableArray<DocumentId> captureMatchingDocuments,
            bool captureAllMatchingDocuments,
            bool reportDiagnostics,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var sessionId = await GetLocalService().StartDebuggingSessionAsync(solution, debuggerService, sourceTextProvider, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics, cancellationToken).ConfigureAwait(false);
                return new RemoteDebuggingSessionProxy(Workspace, LocalConnection.Instance, sessionId);
            }

            // need to keep the providers alive until the session ends:
            var connection = client.CreateConnection<IRemoteEditAndContinueService>(
                callbackTarget: new DebuggingSessionCallback(debuggerService, sourceTextProvider));

            var sessionIdOpt = await connection.TryInvokeAsync(
                solution,
                async (service, solutionInfo, callbackId, cancellationToken) => await service.StartDebuggingSessionAsync(solutionInfo, callbackId, captureMatchingDocuments, captureAllMatchingDocuments, reportDiagnostics, cancellationToken).ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            if (sessionIdOpt.HasValue)
            {
                return new RemoteDebuggingSessionProxy(Workspace, connection, sessionIdOpt.Value);
            }

            connection.Dispose();
            return null;
        }

        public async ValueTask<ImmutableArray<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, Document designTimeDocument, ActiveStatementSpanProvider activeStatementSpanProvider, CancellationToken cancellationToken)
        {
            // filter out documents that are not synchronized to remote process before we attempt remote invoke:
            if (!RemoteSupportedLanguages.IsSupported(document.Project.Language))
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                var diagnostics = await GetLocalService().GetDocumentDiagnosticsAsync(document, activeStatementSpanProvider, cancellationToken).ConfigureAwait(false);

                if (designTimeDocument != document)
                {
                    diagnostics = diagnostics.SelectAsArray(
                        diagnostic => RemapLocation(designTimeDocument, DiagnosticData.Create(document.Project.Solution, diagnostic, document.Project)));
                }

                return diagnostics;
            }

            var diagnosticData = await client.TryInvokeAsync<IRemoteEditAndContinueService, ImmutableArray<DiagnosticData>>(
                document.Project.Solution,
                (service, solutionInfo, callbackId, cancellationToken) => service.GetDocumentDiagnosticsAsync(solutionInfo, callbackId, document.Id, cancellationToken),
                callbackTarget: new ActiveStatementSpanProviderCallback(activeStatementSpanProvider),
                cancellationToken).ConfigureAwait(false);

            if (!diagnosticData.HasValue)
            {
                return ImmutableArray<Diagnostic>.Empty;
            }

            var project = document.Project;

            using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var result);
            foreach (var data in diagnosticData.Value)
            {
                Debug.Assert(data.DataLocation != null);

                Diagnostic diagnostic;

                // Workaround for solution crawler not supporting mapped locations to make Razor work.
                // We pretend the diagnostic is in the original document, but use the mapped line span.
                // Razor will ignore the column (which will be off because #line directives can't currently map columns) and only use the line number.
                if (designTimeDocument != document)
                {
                    diagnostic = RemapLocation(designTimeDocument, data);
                }
                else
                {
                    diagnostic = await data.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false);
                }

                result.Add(diagnostic);
            }

            return result.ToImmutable();
        }

        private static Diagnostic RemapLocation(Document designTimeDocument, DiagnosticData data)
        {
            Debug.Assert(data.DataLocation != null);
            Debug.Assert(designTimeDocument.FilePath != null);

            // If the location in the generated document is in a scope of user-visible #line mapping use the mapped span,
            // otherwise (if it's hidden) display the diagnostic at the start of the file.
            var span = data.DataLocation.UnmappedFileSpan != data.DataLocation.MappedFileSpan ? data.DataLocation.MappedFileSpan.Span : default;
            var location = Location.Create(designTimeDocument.FilePath, textSpan: default, span);

            return data.ToDiagnostic(location, ImmutableArray<Location>.Empty);
        }

        public async ValueTask SetFileLoggingDirectoryAsync(string? logDirectory, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                GetLocalService().SetFileLoggingDirectory(logDirectory);
            }
            else
            {
                await client.TryInvokeAsync<IRemoteEditAndContinueService>(
                    (service, cancellationToken) => service.SetFileLoggingDirectoryAsync(logDirectory, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        private sealed class LocalConnection : IDisposable
        {
            public static readonly LocalConnection Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
