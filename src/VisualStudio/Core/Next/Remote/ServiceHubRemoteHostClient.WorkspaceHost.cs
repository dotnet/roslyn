// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
        private class WorkspaceHost : ForegroundThreadAffinitizedObject, IVisualStudioWorkspaceHost, IVisualStudioWorkingFolder
        {
            private readonly VisualStudioWorkspaceImpl _workspace;
            private readonly RemoteHostClient _client;

            // We have to capture the solution ID because otherwise we won't know
            // what is is when we get told about OnSolutionRemoved.  If we try
            // to access the solution off of _workspace at that point, it will be
            // gone.
            private SolutionId _currentSolutionId;

            public WorkspaceHost(
                VisualStudioWorkspaceImpl workspace,
                RemoteHostClient client)
            {
                _workspace = workspace;
                _client = client;
                _currentSolutionId = workspace.CurrentSolution.Id;

                // Ensure that we populate the remote service with the initial state of
                // the workspace's solution.
                RegisterPrimarySolutionAsync().Wait();
            }

            public void OnAfterWorkingFolderChange()
            {
                this.AssertIsForeground();
                RegisterPrimarySolutionAsync().Wait();
            }

            public void OnSolutionAdded(SolutionInfo solutionInfo)
            {
                this.AssertIsForeground();
                RegisterPrimarySolutionAsync().Wait();
            }

            private async Task RegisterPrimarySolutionAsync()
            {
                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                using (var session = await _client.CreateServiceSessionAsync(WellKnownRemoteHostServices.RemoteHostService, _workspace.CurrentSolution, CancellationToken.None).ConfigureAwait(false))
                {
                    await session.InvokeAsync(
                        WellKnownRemoteHostServices.RemoteHostService_PersistentStorageService_RegisterPrimarySolutionId,
                        solutionId.Id.ToByteArray(),
                        solutionId.DebugName).ConfigureAwait(false);

                    await session.InvokeAsync(
                        WellKnownRemoteHostServices.RemoteHostService_PersistentStorageService_UpdateSolutionIdStorageLocation,
                        solutionId.Id.ToByteArray(),
                        solutionId.DebugName,
                        _workspace.ProjectTracker.GetWorkingFolderPath(_workspace.CurrentSolution)).ConfigureAwait(false);
                }
            }

            public void OnBeforeWorkingFolderChange()
            {
                this.AssertIsForeground();

                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                UnregisterPrimarySolutionAsync(solutionId, synchronousShutdown: true).Wait();
            }

            public void OnSolutionRemoved()
            {
                this.AssertIsForeground();

                // Have to use the cached solution ID we've got as the workspace will
                // no longer have a solution we can look at.
                var solutionId = _currentSolutionId;
                _currentSolutionId = null;

                UnregisterPrimarySolutionAsync(solutionId, synchronousShutdown: false).Wait();
            }

            private async Task UnregisterPrimarySolutionAsync(
                SolutionId solutionId, bool synchronousShutdown)
            {
                using (var session = await _client.CreateServiceSessionAsync(WellKnownRemoteHostServices.RemoteHostService, _workspace.CurrentSolution, CancellationToken.None).ConfigureAwait(false))
                {
                    // ask remote host to sync initial asset
                    await session.InvokeAsync(
                        WellKnownRemoteHostServices.RemoteHostService_PersistentStorageService_UnregisterPrimarySolutionId,
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