// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private readonly HubClient _hubClient;
        private readonly Stream _stream;
        private readonly JsonRpc _rpc;

        public static async Task<RemoteHostClient> CreateAsync(
            Workspace workspace, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.ServiceHubRemoteHostClient_CreateAsync, cancellationToken))
            {
                var primary = new HubClient("ManagedLanguage.IDE.RemoteHostClient");
                var remoteHostStream = await primary.RequestServiceAsync(WellKnownServiceHubServices.RemoteHostService, cancellationToken).ConfigureAwait(false);

                var instance = new ServiceHubRemoteHostClient(workspace, primary, remoteHostStream);

                // make sure connection is done right
                var current = $"VS ({Process.GetCurrentProcess().Id})";
                var host = await instance._rpc.InvokeAsync<string>(WellKnownServiceHubServices.RemoteHostService_Connect, current).ConfigureAwait(false);

                RegisterWorkspaceHost(workspace);

                // TODO: change this to non fatal watson and make VS to use inproc implementation
                Contract.ThrowIfFalse(host == current.ToString());

                instance.Connected();

                // return instance
                return instance;
            }
        }

        private static void RegisterWorkspaceHost(Workspace workspace)
        {
            var vsWorkspace = workspace as VisualStudioWorkspaceImpl;
            if (vsWorkspace == null)
            {
                return;
            }

            vsWorkspace.ProjectTracker.RegisterWorkspaceHost(new WorkspaceHost(vsWorkspace));
        }

        private ServiceHubRemoteHostClient(Workspace workspace, HubClient hubClient, Stream stream) :
            base(workspace)
        {
            _hubClient = hubClient;
            _stream = stream;

            _rpc = JsonRpc.Attach(stream, target: this);

            // handle disconnected situation
            _rpc.Disconnected += OnRpcDisconnected;
        }

        protected override async Task<Session> CreateServiceSessionAsync(string serviceName, ChecksumScope snapshot, object callbackTarget, CancellationToken cancellationToken)
        {
            // get stream from service hub to communicate snapshot/asset related information
            // this is the back channel the system uses to move data between VS and remote host
            var snapshotStream = await _hubClient.RequestServiceAsync(WellKnownServiceHubServices.SnapshotService, cancellationToken).ConfigureAwait(false);

            // get stream from service hub to communicate service specific information
            // this is what consumer actually use to communicate information
            var serviceStream = await _hubClient.RequestServiceAsync(serviceName, cancellationToken).ConfigureAwait(false);

            return new JsonRpcSession(snapshot, snapshotStream, callbackTarget, serviceStream, cancellationToken);
        }

        protected override void OnConnected()
        {
        }

        protected override void OnDisconnected()
        {
            _rpc.Dispose();
            _stream.Dispose();
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected();
        }

        private class WorkspaceHost : IVisualStudioWorkspaceHost, IVisualStudioWorkingFolder
        {
            private readonly VisualStudioWorkspaceImpl _workspace;
            private SolutionId _currentSolutionId;

            public WorkspaceHost(VisualStudioWorkspaceImpl workspace)
            {
                _workspace = workspace;
                _currentSolutionId = workspace.CurrentSolution.Id;
            }

            public void OnBeforeWorkingFolderChange()
            {
                _currentSolutionId = _workspace.CurrentSolution.Id;
                NotifyRemoteHostOfUnregisterPrimarySolution(synchronousShutdown: true);
            }

            public void OnAfterWorkingFolderChange()
            {
                _currentSolutionId = _workspace.CurrentSolution.Id;
                NotifyRemoteHostOfRegisterPrimarySolution();
            }

            public void OnSolutionAdded(SolutionInfo solutionInfo)
            {
                _currentSolutionId = solutionInfo.Id;
                NotifyRemoteHostOfRegisterPrimarySolution();
            }

            public void OnSolutionRemoved()
            {
                NotifyRemoteHostOfUnregisterPrimarySolution(synchronousShutdown: false);
                _currentSolutionId = null;
            }

            private async void NotifyRemoteHostOfRegisterPrimarySolution()
            {
                var solutionId = _currentSolutionId;
                var factory = _workspace.Services.GetService<IRemoteHostClientFactory>();
                var client = await factory.CreateAsync(_workspace, CancellationToken.None).ConfigureAwait(false);

                using (var session = await client.CreateServiceSessionAsync(WellKnownServiceHubServices.RemoteHostService, _workspace.CurrentSolution, CancellationToken.None).ConfigureAwait(false))
                {
                    await session.InvokeAsync(
                        WellKnownServiceHubServices.RemoteHostService_PersistentStorageService_RegisterPrimarySolutionId,
                        solutionId.Id.ToByteArray(),
                        solutionId.DebugName).ConfigureAwait(false);

                    await session.InvokeAsync(
                        WellKnownServiceHubServices.RemoteHostService_PersistentStorageService_UpdateSolutionIdStorageLocation,
                        solutionId.Id.ToByteArray(),
                        solutionId.DebugName,
                        _workspace.ProjectTracker.GetWorkingFolderPath(_workspace.CurrentSolution)).ConfigureAwait(false);
                }
            }

            private async void NotifyRemoteHostOfUnregisterPrimarySolution(bool synchronousShutdown)
            {
                var solutionId = _currentSolutionId;
                var factory = _workspace.Services.GetService<IRemoteHostClientFactory>();
                var client = await factory.CreateAsync(_workspace, CancellationToken.None).ConfigureAwait(false);

                using (var session = await client.CreateServiceSessionAsync(WellKnownServiceHubServices.RemoteHostService, _workspace.CurrentSolution, CancellationToken.None).ConfigureAwait(false))
                {
                    // ask remote host to sync initial asset
                    await session.InvokeAsync(
                        WellKnownServiceHubServices.RemoteHostService_PersistentStorageService_UnregisterPrimarySolutionId,
                        solutionId.Id.ToByteArray(),
                        solutionId.DebugName,
                        synchronousShutdown).ConfigureAwait(false);
                }
            }

            public void ClearSolution() { }
            public void OnAdditionalDocumentAdded(DocumentInfo documentInfo) { }
            public void OnAdditionalDocumentClosed(DocumentId documentId, ITextBuffer textBuffer, TextLoader loader) { }
            public void OnAdditionalDocumentOpened(DocumentId documentId, ITextBuffer textBuffer, bool isCurrentContext) { }
            public void OnAdditionalDocumentRemoved(DocumentId documentInfo) { }
            public void OnAdditionalDocumentTextUpdatedOnDisk(DocumentId id) { }
            public void OnAnalyzerReferenceAdded(ProjectId projectId, AnalyzerReference analyzerReference) { }
            public void OnAnalyzerReferenceRemoved(ProjectId projectId, AnalyzerReference analyzerReference) { }
            public void OnAssemblyNameChanged(ProjectId id, string assemblyName) { }
            public void OnDocumentAdded(DocumentInfo documentInfo) { }
            public void OnDocumentClosed(DocumentId documentId, ITextBuffer textBuffer, TextLoader loader, bool updateActiveContext) { }
            public void OnDocumentOpened(DocumentId documentId, ITextBuffer textBuffer, bool isCurrentContext) { }
            public void OnDocumentRemoved(DocumentId documentId) { }
            public void OnDocumentTextUpdatedOnDisk(DocumentId id) { }
            public void OnMetadataReferenceAdded(ProjectId projectId, PortableExecutableReference metadataReference) { }
            public void OnMetadataReferenceRemoved(ProjectId projectId, PortableExecutableReference metadataReference) { }
            public void OnOptionsChanged(ProjectId projectId, CompilationOptions compilationOptions, ParseOptions parseOptions) { }
            public void OnOutputFilePathChanged(ProjectId id, string outputFilePath) { }
            public void OnProjectAdded(ProjectInfo projectInfo) { }
            public void OnProjectNameChanged(ProjectId projectId, string name, string filePath) { }
            public void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference) { }
            public void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference) { }
            public void OnProjectRemoved(ProjectId projectId) { }
        }
    }
}