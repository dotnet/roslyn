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
            private readonly RemoteHostClient _client;

            /// <summary>
            /// The current connection we have open to the remote host.  Only accessible from the
            /// UI thread.
            /// </summary>
            private ReferenceCountedDisposable<Connection>.WeakReference _currentConnection;

            // We have to capture the solution ID because otherwise we won't know
            // what is is when we get told about OnSolutionRemoved.  If we try
            // to access the solution off of _workspace at that point, it will be
            // gone.
            private SolutionId _currentSolutionId;

            public WorkspaceHost(
                VisualStudioWorkspaceImpl workspace,
                RemoteHostClient client,
                ReferenceCountedDisposable<Connection> currentConnection)
            {
                _workspace = workspace;
                _client = client;
                _currentSolutionId = workspace.CurrentSolution.Id;
                _currentConnection = new ReferenceCountedDisposable<Connection>.WeakReference(currentConnection);
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

            private ReferenceCountedDisposable<Connection> GetOrCreateConnection()
            {
                this.AssertIsForeground();

                // If we have an existing connection, add a ref to it and use that.
                var connectionRef = _currentConnection.TryAddReference();
                if (connectionRef == null)
                {
                    // Otherwise, try to create an actual connection to the OOP server
                    var connection = _client.TryCreateConnectionAsync(WellKnownRemoteHostServices.RemoteHostService, CancellationToken.None)
                                            .WaitAndGetResult(CancellationToken.None);
                    if (connection == null)
                    {
                        return null;
                    }

                    // And set the ref count to it to 1.
                    connectionRef = new ReferenceCountedDisposable<Connection>(connection);
                    _currentConnection = new ReferenceCountedDisposable<Connection>.WeakReference(connectionRef);
                }

                return connectionRef;
            }

            private void RegisterPrimarySolution()
            {
                this.AssertIsForeground();
                _currentSolutionId = _workspace.CurrentSolution.Id;
                var solutionId = _currentSolutionId;

                using (var connection = GetOrCreateConnection())
                {
                    if (connection == null)
                    {
                        // failed to create connection. remote host might not responding or gone. 
                        return;
                    }

                    var storageLocation = _workspace.DeferredState?.ProjectTracker.GetWorkingFolderPath(_workspace.CurrentSolution);

                    connection.Target.InvokeAsync(
                        nameof(IRemoteHostService.RegisterPrimarySolutionId),
                        new object[] { solutionId, storageLocation }, CancellationToken.None).Wait(CancellationToken.None);
                }
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
                _client.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService, _workspace.CurrentSolution,
                    nameof(IRemoteHostService.UnregisterPrimarySolutionId), new object[] { solutionId, synchronousShutdown },
                    CancellationToken.None).Wait(CancellationToken.None);
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
