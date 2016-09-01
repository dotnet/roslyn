// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
        private class WorkspaceHost : ForegroundThreadAffinitizedObject, IVisualStudioWorkspaceHost, IVisualStudioWorkingFolder
        {
            private readonly VisualStudioWorkspaceImpl _workspace;

            // We have to capture the solution ID because otherwise we won't know
            // what is is when we get told about OnSolutionRemoved.  If we try
            // to access the solution off of _workspace at that point, it will be
            // gone.
            private SolutionId _currentSolutionId;

            // We chain all tasks so that all the messages we send to the remote 
            // process are serialized.  We don't want to interleave the individual
            // messages we have for things like registering/unregistering the 
            // primary solution ID.
            private readonly object _gate = new object();
            private Task _lastTask = SpecializedTasks.EmptyTask;

            public WorkspaceHost(VisualStudioWorkspaceImpl workspace)
            {
                _workspace = workspace;
                _currentSolutionId = workspace.CurrentSolution.Id;
            }

            public void OnBeforeWorkingFolderChange()
            {
                this.AssertIsForeground();

                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                lock (_gate)
                {
                    _lastTask = _lastTask.ContinueWith(
                        _ => NotifyRemoteHostOfUnregisterPrimarySolution(solutionId, synchronousShutdown: true),
                        TaskScheduler.Default).Unwrap();
                }
            }

            public void OnAfterWorkingFolderChange()
            {
                this.AssertIsForeground();

                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                lock (_gate)
                {
                    _lastTask = _lastTask.ContinueWith(
                        _ => NotifyRemoteHostOfRegisterPrimarySolution(solutionId),
                        TaskScheduler.Default).Unwrap();
                }
            }

            public void OnSolutionAdded(SolutionInfo solutionInfo)
            {
                this.AssertIsForeground();

                _currentSolutionId = solutionInfo.Id;
                var solutionId = _currentSolutionId;

                lock (_gate)
                {
                    _lastTask = _lastTask.ContinueWith(
                        _ => NotifyRemoteHostOfRegisterPrimarySolution(solutionId),
                        TaskScheduler.Default).Unwrap();
                }
            }

            public void OnSolutionRemoved()
            {
                this.AssertIsForeground();

                // Have to use the cached solution ID we've got as the workspace will
                // no longer have a solution we can look at.
                var solutionId = _currentSolutionId;
                _currentSolutionId = null;

                lock (_gate)
                {
                    _lastTask = _lastTask.ContinueWith(
                        _ => NotifyRemoteHostOfUnregisterPrimarySolution(solutionId, synchronousShutdown: false),
                        TaskScheduler.Default).Unwrap();
                }
            }

            public async Task NotifyRemoteHostOfRegisterPrimarySolution(SolutionId solutionId)
            {
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

            private async Task NotifyRemoteHostOfUnregisterPrimarySolution(
                SolutionId solutionId, bool synchronousShutdown)
            {
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