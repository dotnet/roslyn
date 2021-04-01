// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Api;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageClient
{
    /// <summary>
    /// Implementation of <see cref="InProcLanguageServer"/> that also supports
    /// VS LSP extension methods.
    /// </summary>
    internal class VisualStudioInProcLanguageServer : InProcLanguageServer
    {
        internal VisualStudioInProcLanguageServer(
            AbstractRequestDispatcherFactory requestDispatcherFactory,
            JsonRpc jsonRpc,
            ServerCapabilities serverCapabilities,
            ILspWorkspaceRegistrationService workspaceRegistrationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ILspLogger logger,
            IDiagnosticService? diagnosticService,
            string? clientName,
            string userVisibleServerName,
            string telemetryServerTypeName) : base(requestDispatcherFactory, jsonRpc, serverCapabilities, workspaceRegistrationService, listenerProvider, logger, diagnosticService, clientName, userVisibleServerName, telemetryServerTypeName)
        {
        }

        [JsonRpcMethod(MSLSPMethods.TextDocumentCodeActionResolveName, UseSingleObjectParameterDeserialization = true)]
        public Task<VSCodeAction> ResolveCodeActionAsync(VSCodeAction vsCodeAction, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<VSCodeAction, VSCodeAction>(Queue, MSLSPMethods.TextDocumentCodeActionResolveName,
                vsCodeAction, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.DocumentPullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<DiagnosticReport[]?> GetDocumentPullDiagnosticsAsync(DocumentDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentDiagnosticsParams, DiagnosticReport[]?>(
                Queue, MSLSPMethods.DocumentPullDiagnosticName,
                diagnosticsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.WorkspacePullDiagnosticName, UseSingleObjectParameterDeserialization = true)]
        public Task<WorkspaceDiagnosticReport[]?> GetWorkspacePullDiagnosticsAsync(WorkspaceDocumentDiagnosticsParams diagnosticsParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<WorkspaceDocumentDiagnosticsParams, WorkspaceDiagnosticReport[]?>(
                Queue, MSLSPMethods.WorkspacePullDiagnosticName,
                diagnosticsParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.ProjectContextsName, UseSingleObjectParameterDeserialization = true)]
        public Task<ActiveProjectContexts?> GetProjectContextsAsync(GetTextDocumentWithContextParams textDocumentWithContextParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<GetTextDocumentWithContextParams, ActiveProjectContexts?>(Queue, MSLSPMethods.ProjectContextsName,
                textDocumentWithContextParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.OnAutoInsertName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentOnAutoInsertResponseItem?> GetDocumentOnAutoInsertAsync(DocumentOnAutoInsertParams autoInsertParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentOnAutoInsertParams, DocumentOnAutoInsertResponseItem?>(Queue, MSLSPMethods.OnAutoInsertName,
                autoInsertParams, _clientCapabilities, ClientName, cancellationToken);
        }

        [JsonRpcMethod(MSLSPMethods.OnTypeRenameName, UseSingleObjectParameterDeserialization = true)]
        public Task<DocumentOnTypeRenameResponseItem?> GetTypeRenameAsync(DocumentOnTypeRenameParams renameParams, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_clientCapabilities, $"{nameof(InitializeAsync)} has not been called.");

            return RequestDispatcher.ExecuteRequestAsync<DocumentOnTypeRenameParams, DocumentOnTypeRenameResponseItem?>(Queue, MSLSPMethods.OnTypeRenameName,
                renameParams, _clientCapabilities, ClientName, cancellationToken);
        }
    }
}
