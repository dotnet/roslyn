using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Services.Host
{
    /// <summary>
    /// A base class used by Hosts to define host specific Workspaces.
    /// </summary>
    public abstract partial class HostWorkspace : TrackingWorkspace
    {
        // forces serialization of mutation calls from host (OnXXX methods). Must take this lock before taking stateLock.
        private readonly NonReentrantLock serializationLock = new NonReentrantLock();
        private readonly IFileTracker fileTracker;

        protected HostWorkspace(
            IWorkspaceServiceProvider workspaceServiceProvider,
            bool enableBackgroundCompilation = true,
            bool enableInProgressSolutions = true,
            bool enableFileTracking = false)
            : base(
                workspaceServiceProvider,
                enableBackgroundCompilation,
                enableInProgressSolutions)
        {
            if (enableFileTracking)
            {
                var fileTrackingService = workspaceServiceProvider.GetService<IFileTrackingService>();
                this.fileTracker = fileTrackingService.CreateFileTracker();
            }
        }

        protected override void ClearSolution()
        {
            base.ClearSolution();
        }

        protected internal void OnSolutionAdded(ISolutionInfo solutionInfo)
        {
            var solutionId = solutionInfo.Id;

            using (this.serializationLock.DisposableWait())
            {
                CheckSolutionIsEmpty();
                this.SetLatestSolution(this.CreateSolution(solutionInfo));
            }

            solutionInfo.Projects.Do(OnProjectAdded);

            var newSolution = this.CurrentSolution;

            if (this.fileTracker != null && solutionInfo.FilePath != null)
            {
                this.fileTracker.Track(solutionInfo.FilePath, delegate { this.OnSolutionFileChanged(solutionId); });
            }

            this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.SolutionAdded, newSolution);
        }

        protected internal void OnSolutionReloaded(ISolutionInfo reloadedSolutionInfo)
        {
            var solutionId = reloadedSolutionInfo.Id;
            var oldSolution = this.CurrentSolution;
            ISolution newSolution;

            using (this.serializationLock.DisposableWait())
            {
                newSolution = this.CreateSolution(reloadedSolutionInfo);
                this.SetLatestSolution(newSolution);
            }

            reloadedSolutionInfo.Projects.Do(pi => OnProjectAdded(pi, silent: true));

            var newSolution2 = this.CurrentSolution;

            // keep open documents using same text
            foreach (var docId in this.GetOpenDocumentIds())
            {
                if (newSolution2.ContainsDocument(docId))
                {
                    newSolution2 = newSolution2.UpdateDocument(docId, oldSolution.GetDocument(docId).GetText());
                }
            }

            this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.SolutionReloaded, newSolution);
        }

        protected internal void OnSolutionRemoved(SolutionId solutionId)
        {
            using (this.serializationLock.DisposableWait())
            {
                var oldSolution = this.CurrentSolution;

                this.ClearSolution();

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.SolutionRemoved, oldSolution);
            }
        }

        protected internal void OnProjectAdded(IProjectInfo projectInfo)
        {
            this.OnProjectAdded(projectInfo, silent: false);
        }

        private void OnProjectAdded(IProjectInfo projectInfo, bool silent)
        {
            using (this.serializationLock.DisposableWait())
            {
                var projectId = projectInfo.Id;

                CheckProjectIsNotInCurrentSolution(projectId);

                var newSolution = this.CurrentSolution.AddProject(new RegisteringProjectInfo(this, projectInfo));

                this.SetLatestSolution(newSolution);

                if (this.fileTracker != null && projectInfo.FilePath != null)
                {
                    this.fileTracker.Track(projectInfo.FilePath, delegate
                    {
                        this.OnProjectFileChanged(projectId);
                    });
                }

                if (!silent)
                {
                    this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectAdded, newSolution, projectId);
                }

                // NOTE: don't add documents and references directly, they are deferred.
            }
        }

        protected internal void OnProjectReloaded(IProjectInfo reloadedProjectInfo)
        {
            using (this.serializationLock.DisposableWait())
            {
                var projectId = reloadedProjectInfo.Id;

                CheckProjectIsInCurrentSolution(projectId);

                var oldSolution = this.CurrentSolution;
                var newSolution = oldSolution.RemoveProject(projectId).AddProject(new RegisteringProjectInfo(this, reloadedProjectInfo));

                // keep open documents open using same text
                foreach (var docId in this.GetOpenDocumentIds(projectId))
                {
                    if (newSolution.ContainsDocument(docId))
                    {
                        newSolution = newSolution.UpdateDocument(docId, oldSolution.GetDocument(docId).GetText());
                    }
                }

                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectReloaded, newSolution, projectId);
            }
        }

        // this class registers all the host documents with the workspace the first time they are observed
        private class RegisteringProjectInfo : DelegatedProjectInfo
        {
            private readonly IProjectInfo projectInfo;
            private readonly NonReentrantLazy<ImmutableList<IDocumentInfo>> lazyDocuments;

            public RegisteringProjectInfo(HostWorkspace workspace, IProjectInfo projectInfo)
            {
                this.projectInfo = projectInfo;
                this.lazyDocuments = NonReentrantLazy.Create(() =>
                {
                    var docs = projectInfo.Documents.ToImmutableList();
                    docs.Do(d => workspace.RegisterHostDocument(d));
                    return docs;
                });
            }

            protected override IProjectInfo ProjectInfo
            {
                get { return this.projectInfo; }
            }

            public override IEnumerable<IDocumentInfo> Documents
            {
                get { return this.lazyDocuments.Value; }
            }
        }

        protected internal void OnProjectRemoved(ProjectId projectId)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                CheckProjectDoesNotContainOpenDocuments(projectId);

                this.ClearProject(projectId);

                var newSolution = this.CurrentSolution.RemoveProject(projectId);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectRemoved, newSolution, projectId);
            }
        }

        protected internal void OnAssemblyNameUpdated(ProjectId projectId, string assemblyName)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);

                var newSolution = this.CurrentSolution.UpdateAssemblyName(projectId, assemblyName);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnCompilationOptionsUpdated(ProjectId projectId, CommonCompilationOptions options)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);

                var newSolution = this.CurrentSolution.UpdateCompilationOptions(projectId, options);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnParseOptionsUpdated(ProjectId projectId, CommonParseOptions options)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);

                var newSolution = this.CurrentSolution.UpdateParseOptions(projectId, options);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnFileResolverUpdated(ProjectId projectId, FileResolver fileResolver)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                var newSolution = this.CurrentSolution.UpdateFileResolver(projectId, fileResolver);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnProjectReferenceAdded(ProjectId projectId, ProjectReference projectReference)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                CheckProjectIsInCurrentSolution(projectReference.ProjectId);
                CheckProjectDoesNotHaveProjectReference(projectId, projectReference.ProjectId);

                // Can only add this P2P reference if it would not cause a circularity.
                CheckProjectDoesNotHaveTransitiveProjectReference(projectId, projectReference.ProjectId);

                var newSolution = this.CurrentSolution.AddProjectReference(projectId, projectReference);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnProjectReferenceRemoved(ProjectId projectId, ProjectReference projectReference)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                CheckProjectIsInCurrentSolution(projectReference.ProjectId);
                CheckProjectHasProjectReference(projectId, projectReference.ProjectId);

                var newSolution = this.CurrentSolution.RemoveProjectReference(projectId, projectReference);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnMetadataReferenceAdded(ProjectId projectId, MetadataReference metadataReference)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                CheckProjectDoesNotHaveMetadataReference(projectId, metadataReference);

                var newSolution = this.CurrentSolution.AddMetadataReference(projectId, metadataReference);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnMetadataReferenceRemoved(ProjectId projectId, MetadataReference metadataReference)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckProjectIsInCurrentSolution(projectId);
                CheckProjectHasMetadataReference(projectId, metadataReference);

                var newSolution = this.CurrentSolution.RemoveMetadataReference(projectId, metadataReference);
                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.ProjectUpdated, newSolution, projectId);
            }
        }

        protected internal void OnDocumentAdded(IDocumentInfo documentInfo)
        {
            using (this.serializationLock.DisposableWait())
            {
                var documentId = documentInfo.Id;

                CheckProjectIsInCurrentSolution(documentId.ProjectId);
                CheckDocumentIsNotInCurrentSolution(documentId);

                RegisterHostDocument(documentInfo);

                var newSolution = this.CurrentSolution.AddDocument(documentInfo);

                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.DocumentAdded, newSolution, documentId: documentId);
            }
        }

        protected internal void OnDocumentReloaded(IDocumentInfo newDocumentInfo)
        {
            using (this.serializationLock.DisposableWait())
            {
                var documentId = newDocumentInfo.Id;

                CheckProjectIsInCurrentSolution(documentId.ProjectId);
                CheckDocumentIsInCurrentSolution(documentId);

                RegisterHostDocument(newDocumentInfo);

                var newSolution = this.CurrentSolution.RemoveDocument(documentId).AddDocument(newDocumentInfo);

                this.SetLatestSolution(newSolution);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.DocumentReloaded, newSolution, documentId: documentId);
            }
        }

        private void RegisterHostDocument(IDocumentInfo documentInfo)
        {
            var documentId = documentInfo.Id;

            if (this.fileTracker != null && documentInfo.FilePath != null)
            {
                this.fileTracker.Track(documentInfo.FilePath, delegate { this.OnDocumentFileChanged(documentId); });
            }
        }

        protected internal void OnDocumentRemoved(DocumentId documentId)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);

                CheckDocumentIsClosed(documentId);

                var newSolution = this.CurrentSolution.RemoveDocument(documentId);
                this.SetLatestSolution(newSolution);

                this.ClearDocument(documentId);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.DocumentRemoved, newSolution, documentId: documentId);
            }
        }

        protected internal void OnDocumentTextUpdated(DocumentId documentId)
        {
            using (this.serializationLock.DisposableWait())
            {
                CheckDocumentIsInCurrentSolution(documentId);

                var newSolution = this.CurrentSolution.ReloadDocument(documentId);
                this.SetLatestSolution(newSolution);

                var newDocument = newSolution.GetDocument(documentId);
                this.BackgroundParse(newDocument);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.DocumentUpdated, newSolution, documentId: documentId);
            }
        }

        /// <summary>
        /// Called when file change tracking is enabled and the solution file changes on disk.
        /// </summary>
        protected virtual void OnSolutionFileChanged(SolutionId solutionId)
        {
        }

        /// <summary>
        /// Called when file change tracking is enabled and the project file changes on disk.
        /// </summary>
        protected virtual void OnProjectFileChanged(ProjectId projectId)
        {
        }

        /// <summary>
        /// Called when file change tracking is enabled and the document file changes on disk.
        /// </summary>
        protected virtual void OnDocumentFileChanged(DocumentId documentId)
        {
        }

        protected override void OnDisposed()
        {
            base.OnDisposed();

            if (this.fileTracker != null)
            {
                this.fileTracker.Dispose();
            }
        }
    }
}
