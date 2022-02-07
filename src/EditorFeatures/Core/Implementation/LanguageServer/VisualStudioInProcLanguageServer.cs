// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient
{
    /// <summary>
    /// Implementation of <see cref="LanguageServerTarget"/> that also supports
    /// VS LSP extension methods.
    /// </summary>
    internal class VisualStudioInProcLanguageServer : LanguageServerTarget
    {
        private readonly ImmutableArray<string> _supportedLanguages;

        internal VisualStudioInProcLanguageServer(
            AbstractRequestDispatcherFactory requestDispatcherFactory,
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            LspWorkspaceRegistrationService workspaceRegistrationService,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            ImmutableArray<string> supportedLanguages,
            string? clientName,
            WellKnownLspServerKinds serverKind)
            : base(requestDispatcherFactory, jsonRpc, capabilitiesProvider, workspaceRegistrationService, lspMiscellaneousFilesWorkspace: null, globalOptions, listenerProvider, logger, supportedLanguages, clientName, serverKind)
        {
            _supportedLanguages = supportedLanguages;
        }

        public override Task InitializedAsync()
        {
            return Task.CompletedTask;
        }

        [JsonRpcMethod(VSInternalMethods.DocumentPullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalDiagnosticReport[]?> GetDocumentPullDiagnosticsAsync(VSInternalDocumentDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalDocumentDiagnosticsParams, VSInternalDiagnosticReport[]?>(
                Queue, VSInternalMethods.DocumentPullDiagnosticName,
                diagnosticsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSInternalMethods.WorkspacePullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalWorkspaceDiagnosticReport[]?> GetWorkspacePullDiagnosticsAsync(VSInternalWorkspaceDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalWorkspaceDiagnosticsParams, VSInternalWorkspaceDiagnosticReport[]?>(
                Queue, VSInternalMethods.WorkspacePullDiagnosticName,
                diagnosticsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSMethods.GetProjectContextsName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSProjectContextList?> GetProjectContextsAsync(VSGetProjectContextsParams textDocumentWithContextParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSGetProjectContextsParams, VSProjectContextList?>(Queue, VSMethods.GetProjectContextsName,
                textDocumentWithContextParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSInternalMethods.OnAutoInsertName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalDocumentOnAutoInsertResponseItem?> GetDocumentOnAutoInsertAsync(VSInternalDocumentOnAutoInsertParams autoInsertParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalDocumentOnAutoInsertParams, VSInternalDocumentOnAutoInsertResponseItem?>(Queue, VSInternalMethods.OnAutoInsertName,
                autoInsertParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(Methods.TextDocumentLinkedEditingRangeName, UseSingleObjectParameterDeserialization = true)]
        public Task<LinkedEditingRanges?> GetLinkedEditingRangesAsync(LinkedEditingRangeParams renameParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<LinkedEditingRangeParams, LinkedEditingRanges?>(Queue, Methods.TextDocumentLinkedEditingRangeName,
                renameParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(VSInternalMethods.TextDocumentInlineCompletionName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSInternalInlineCompletionList?> GetInlineCompletionsAsync(VSInternalInlineCompletionRequest request, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSInternalInlineCompletionRequest, VSInternalInlineCompletionList?>(Queue, VSInternalMethods.TextDocumentInlineCompletionName,
                request, _clientCapabilities, ClientName, cancellationToken);
        }
    }
}
