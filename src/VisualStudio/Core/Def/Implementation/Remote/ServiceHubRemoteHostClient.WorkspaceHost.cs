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

            /// <summary>
            /// The current session we have open to the remote host.  Only accessible from the
            /// UI thread.
            /// </summary>
            private readonly KeepAliveSession _session;

            // We have to capture the solution ID because otherwise we won't know
            // what is is when we get told about OnSolutionRemoved.  If we try
            // to access the solution off of _workspace at that point, it will be
            // gone.
            private SolutionId _currentSolutionId;

            /// <summary>
            /// A task that represents the most recent queued work to notify the remote process
            /// about working folder paths.
            /// </summary>
            private Task _currentRemoteWorkspaceNotificationTask = Task.CompletedTask;

            public WorkspaceHost(VisualStudioWorkspaceImpl workspace, KeepAliveSession session)
            {
                _workspace = workspace;
                _currentSolutionId = workspace.CurrentSolution.Id;
                _session = session;
            }

            public void OnAfterWorkingFolderChange()
            {
                this.AssertIsForeground();
                RegisterPrimarySolution();
            }

            public void OnSolutionAdded(SolutionInfo solutionInfo)
            {
                this.AssertIsForeground();
                RegisterPrimarySolution();
            }

            private void RegisterPrimarySolution()
            {
                this.AssertIsForeground();
                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                var storageLocation = _workspace.DeferredState?.ProjectTracker.GetWorkingFolderPath(_workspace.CurrentSolution);

                _currentRemoteWorkspaceNotificationTask = _currentRemoteWorkspaceNotificationTask.SafeContinueWithFromAsync(_ =>
                    {
                        return _session.TryInvokeAsync(
                            nameof(IRemoteHostService.RegisterPrimarySolutionId),
                            new object[] { solutionId, storageLocation }, CancellationToken.None);
                    }, CancellationToken.None, TaskScheduler.Default);
            }

            public void OnBeforeWorkingFolderChange()
            {
                this.AssertIsForeground();

                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                UnregisterPrimarySolution(solutionId, synchronousShutdown: true);
            }

            public void OnSolutionRemoved()
            {
                this.AssertIsForeground();

                // Have to use the cached solution ID we've got as the workspace will
                // no longer have a solution we can look at.
                var solutionId = _currentSolutionId;
                _currentSolutionId = null;

                UnregisterPrimarySolution(solutionId, synchronousShutdown: false);
            }

            private void UnregisterPrimarySolution(
                SolutionId solutionId, bool synchronousShutdown)
            {
                _currentRemoteWorkspaceNotificationTask = _currentRemoteWorkspaceNotificationTask.SafeContinueWith(_ =>
                    {
                        return _session.TryInvokeAsync(
                            nameof(IRemoteHostService.UnregisterPrimarySolutionId),
                            new object[] { solutionId, synchronousShutdown },
                            CancellationToken.None);
                    }, CancellationToken.None, TaskScheduler.Default);

                if (synchronousShutdown)
                {
                    _currentRemoteWorkspaceNotificationTask.Wait();
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
