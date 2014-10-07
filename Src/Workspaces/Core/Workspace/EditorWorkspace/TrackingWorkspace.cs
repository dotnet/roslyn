using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Services.Host;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    /// <summary>
    /// A workspace that tracks changes to open documents being editted.
    /// </summary>
    public abstract partial class TrackingWorkspace : Workspace
    {
        // protects mutable collections
        private readonly ReaderWriterLockSlim stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        // open documents
        private readonly Dictionary<ProjectId, ISet<DocumentId>> projectToOpenDocumentsMap = new Dictionary<ProjectId, ISet<DocumentId>>();

        // text buffer maps
        private readonly Dictionary<ITextContainer, DocumentId> bufferToDocumentIdMap = new Dictionary<ITextContainer, DocumentId>();
        private readonly Dictionary<DocumentId, TextTracker> textTrackers = new Dictionary<DocumentId, TextTracker>();

        protected TrackingWorkspace(
            IWorkspaceServiceProvider workspaceServiceProvider,
            bool enableBackgroundCompilation = true,
            bool enableFileTracking = false)
            : base(
                workspaceServiceProvider,
                enableBackgroundCompilation,
                enableFileTracking)
        {
        }

        private static void RemoveIfEmpty<TKey, TValue>(IDictionary<TKey, ISet<TValue>> dictionary, TKey key)
        {
            ISet<TValue> values;
            if (dictionary.TryGetValue(key, out values))
            {
                if (values.Count == 0)
                {
                    dictionary.Remove(key);
                }
            }
        }

        protected override void ClearSolutionData()
        {
            // Clear any known project & document state
            this.ClearOpenDocuments();
            base.ClearSolutionData();
        }

        protected override void ClearProjectData(ProjectId projectId)
        {
            this.ClearOpenDocuments(projectId);
        }

        protected override void ClearDocumentData(DocumentId documentId)
        {
            this.ClearOpenDocument(documentId);
        }

        private void ClearOpenDocuments()
        {
            List<DocumentId> docIds;
            using (this.stateLock.DisposableRead())
            {
                docIds = this.projectToOpenDocumentsMap.Values.SelectMany(x => x).ToList();
            }

            foreach (var docId in docIds)
            {
                this.ClearOpenDocument(docId);
            }
        }

        private void ClearOpenDocuments(ProjectId projectId)
        {
            ISet<DocumentId> openDocs;
            using (this.stateLock.DisposableRead())
            {
                this.projectToOpenDocumentsMap.TryGetValue(projectId, out openDocs);
            }

            if (openDocs != null)
            {
                foreach (var docId in openDocs)
                {
                    this.ClearOpenDocument(docId);
                }
            }
        }

        // TODO (tomat): This is public to enable removing mapping between buffer and document in REPL.
        // Is there a better way to do it? Perhaps TrackingWorkspace should implement RemoveDocumentAsync?
        public void ClearOpenDocument(DocumentId documentId)
        {
            using (this.stateLock.DisposableWrite())
            {
                this.ClearOpenDocument_NoLock(documentId);
            }
        }

        private void ClearOpenDocument_NoLock(DocumentId documentId)
        {
            this.stateLock.AssertCanWrite();

            ISet<DocumentId> openDocIds;

            if (this.projectToOpenDocumentsMap.TryGetValue(documentId.ProjectId, out openDocIds) && openDocIds != null)
            {
                openDocIds.Remove(documentId);
            }

            RemoveIfEmpty(this.projectToOpenDocumentsMap, documentId.ProjectId);

            // forget the buffer!
            TextTracker tracker;
            if (this.textTrackers.TryGetValue(documentId, out tracker))
            {
                tracker.Disconnect();
                this.textTrackers.Remove(documentId);

                this.bufferToDocumentIdMap.Remove(tracker.TextContainer);
                Workspace.UnregisterText(tracker.TextContainer);
            }
        }

        protected override void CheckProjectDoesNotContainOpenDocuments(ProjectId projectId)
        {
            using (this.stateLock.DisposableRead())
            {
                if (this.projectToOpenDocumentsMap.ContainsKey(projectId))
                {
                    throw new ArgumentException(string.Format("{0} still contains open documents.", this.GetProjectName(projectId)));
                }
            }
        }

        public override bool IsDocumentOpen(DocumentId documentId)
        {
            using (this.stateLock.DisposableRead())
            {
                var openDocuments = this.GetProjectOpenDocuments_NoLock(documentId.ProjectId);
                return openDocuments != null && openDocuments.Contains(documentId);
            }
        }

        public override IEnumerable<DocumentId> GetOpenedDocuments()
        {
            using (this.stateLock.DisposableRead())
            {
                if (this.projectToOpenDocumentsMap.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerable<DocumentId>();
                }
                else
                {
                    return this.projectToOpenDocumentsMap.SelectMany(kvp => kvp.Value).ToImmutableList();
                }
            }
        }

        protected override void CheckDocumentIsClosed(DocumentId documentId)
        {
            if (this.IsDocumentOpen(documentId))
            {
                throw new ArgumentException(
                    string.Format("{0} is still open.",
                    this.GetDocumentName(documentId)));
            }
        }

        protected override void CheckDocumentIsOpen(DocumentId documentId)
        {
            if (!this.IsDocumentOpen(documentId))
            {
                throw new ArgumentException(string.Format(
                    "'{0}' is not open.".NeedsLocalization(),
                    this.GetDocumentName(documentId)));
            }
        }

        private ISet<DocumentId> GetProjectOpenDocuments_NoLock(ProjectId project)
        {
            this.stateLock.AssertCanRead();

            ISet<DocumentId> openDocs;

            projectToOpenDocumentsMap.TryGetValue(project, out openDocs);
            return openDocs;
        }

        protected override ICollection<DocumentId> GetOpenDocumentIds(ProjectId projectId = null)
        {
            using (this.stateLock.DisposableRead())
            {
                if (projectId != null)
                {
                    ISet<DocumentId> openProjectDocs;
                    if (this.projectToOpenDocumentsMap.TryGetValue(projectId, out openProjectDocs))
                    {
                        return openProjectDocs.ToImmutableList();
                    }
                }
                else if (this.projectToOpenDocumentsMap.Count > 0)
                {
                    return projectToOpenDocumentsMap.SelectMany(kv => kv.Value).ToImmutableList();
                }

                return ImmutableList<DocumentId>.Empty;
            }
        }

        protected internal void OnDocumentOpened(DocumentId documentId, ITextContainer textContainer)
        {
            ISolution newSolution;

            CheckDocumentIsInCurrentSolution(documentId);
            CheckDocumentIsClosed(documentId);

            using (this.stateLock.DisposableWrite())
            {
                var oldDocument = this.CurrentSolution.GetDocument(documentId);
                var openDocuments = GetProjectOpenDocuments_NoLock(documentId.ProjectId);
                if (openDocuments != null)
                {
                    openDocuments.Add(documentId);
                }
                else
                {
                    this.projectToOpenDocumentsMap.Add(documentId.ProjectId, new HashSet<DocumentId> { documentId });
                }

                // keep open document text alive by using PreserveIdentity
                newSolution = this.CurrentSolution.UpdateDocument(documentId, textContainer.CurrentText, PreservationMode.PreserveIdentity);
                this.SetCurrentSolution(newSolution);

                var tracker = new TextTracker(this, documentId, textContainer);
                this.textTrackers.Add(documentId, tracker);
                this.bufferToDocumentIdMap.Add(textContainer, documentId);
                tracker.Connect();

                var newDoc = newSolution.GetDocument(documentId);
                this.BackgroundParse(newDoc);

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.DocumentOpened, newSolution, documentId: documentId);
            }

            // register outside of lock since it may call user code.
            Workspace.RegisterText(textContainer, this);
        }

        protected internal void OnDocumentClosed(DocumentId documentId, TextLoader reloader)
        {
            this.CheckDocumentIsInCurrentSolution(documentId);
            this.CheckDocumentIsOpen(documentId);

            using (this.stateLock.DisposableWrite())
            {
                // forget any open document info
                this.ClearOpenDocument_NoLock(documentId);

                var oldDocument = this.CurrentSolution.GetDocument(documentId);

                var newSolution = this.CurrentSolution.UpdateDocument(documentId, reloader);
                this.SetCurrentSolution(newSolution);

                this.CancelBackgroundParse(oldDocument);
                this.BackgroundParse(newSolution.GetDocument(documentId));

                this.RaiseWorkspaceChangedEventAsync(WorkspaceEventKind.DocumentClosed, newSolution, documentId: documentId);
            }
        }

        public override DocumentId GetDocumentId(ITextContainer textContainer)
        {
            if (textContainer == null)
            {
                throw new ArgumentNullException("textContainer");
            }

            using (this.stateLock.DisposableRead())
            {
                DocumentId documentId;
                this.bufferToDocumentIdMap.TryGetValue(textContainer, out documentId);
                return documentId;
            }
        }
    }
}