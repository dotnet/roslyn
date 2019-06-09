// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class OpenDocumentTracker<TItem>
        where TItem : TableItem
    {
        private readonly object _gate = new object();
        private readonly Dictionary<DocumentId, Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<TItem>>>> _map =
            new Dictionary<DocumentId, Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<TItem>>>>();

        private readonly Workspace _workspace;

        public OpenDocumentTracker(Workspace workspace)
        {
            _workspace = workspace;

            _workspace.DocumentClosed += OnDocumentClosed;
            _workspace.WorkspaceChanged += OnWorkspaceChanged;
        }

        public void TrackOpenDocument(DocumentId documentId, object id, AbstractTableEntriesSnapshot<TItem> snapshot)
        {
            lock (_gate)
            {
                if (!_map.TryGetValue(documentId, out var secondMap))
                {
                    secondMap = new Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<TItem>>>();
                    _map.Add(documentId, secondMap);
                }

                if (secondMap.TryGetValue(id, out var oldWeakSnapshot) && oldWeakSnapshot.TryGetTarget(out var oldSnapshot))
                {
                    oldSnapshot.StopTracking();
                }

                secondMap[id] = new WeakReference<AbstractTableEntriesSnapshot<TItem>>(snapshot);
            }
        }

        private void StopTracking(DocumentId documentId)
        {
            lock (_gate)
            {
                StopTracking_NoLock(documentId);
            }
        }

        private void StopTracking(Solution solution, ProjectId projectId = null)
        {
            lock (_gate)
            {
                foreach (var documentId in _map.Keys.Where(d => projectId == null ? true : d.ProjectId == projectId).ToList())
                {
                    if (solution.GetDocument(documentId) != null)
                    {
                        // document still exist.
                        continue;
                    }

                    StopTracking_NoLock(documentId);
                }
            }
        }

        private void StopTracking_NoLock(DocumentId documentId)
        {
            if (!_map.TryGetValue(documentId, out var secondMap))
            {
                return;
            }

            _map.Remove(documentId);
            foreach (var weakSnapshot in secondMap.Values)
            {
                if (!weakSnapshot.TryGetTarget(out var snapshot))
                {
                    continue;
                }

                snapshot.StopTracking();
            }
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs e)
        {
            switch (e.Kind)
            {
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionCleared:
                    StopTracking(e.NewSolution);
                    break;

                case WorkspaceChangeKind.ProjectRemoved:
                    StopTracking(e.NewSolution, e.ProjectId);
                    break;

                case WorkspaceChangeKind.DocumentRemoved:
                    StopTracking(e.DocumentId);
                    break;

                default:
                    // do nothing
                    break;
            }
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            StopTracking(e.Document.Id);
        }
    }
}
