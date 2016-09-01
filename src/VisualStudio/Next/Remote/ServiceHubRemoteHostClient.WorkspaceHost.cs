// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
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