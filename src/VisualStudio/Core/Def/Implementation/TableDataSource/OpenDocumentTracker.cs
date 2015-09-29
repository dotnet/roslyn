// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class OpenDocumentTracker<T>
    {
        private readonly object _gate = new object();
        private readonly Dictionary<DocumentId, Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<T>>>> _map =
            new Dictionary<DocumentId, Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<T>>>>();

        private readonly Workspace _workspace;

        public OpenDocumentTracker(Workspace workspace)
        {
            _workspace = workspace;

            _workspace.DocumentClosed += OnDocumentClosed;
        }

        public void TrackOpenDocument(DocumentId documentId, object id, AbstractTableEntriesSnapshot<T> snapshot)
        {
            lock (_gate)
            {
                Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<T>>> secondMap;
                if (!_map.TryGetValue(documentId, out secondMap))
                {
                    secondMap = new Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<T>>>();
                    _map.Add(documentId, secondMap);
                }

                AbstractTableEntriesSnapshot<T> oldSnapshot;
                WeakReference<AbstractTableEntriesSnapshot<T>> oldWeakSnapshot;
                if (secondMap.TryGetValue(id, out oldWeakSnapshot) && oldWeakSnapshot.TryGetTarget(out oldSnapshot))
                {
                    oldSnapshot.StopTracking();
                }

                secondMap[id] = new WeakReference<AbstractTableEntriesSnapshot<T>>(snapshot);
            }
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<T>>> secondMap;
                if (!_map.TryGetValue(e.Document.Id, out secondMap))
                {
                    return;
                }

                _map.Remove(e.Document.Id);
                foreach (var weakSnapshot in secondMap.Values)
                {
                    AbstractTableEntriesSnapshot<T> snapshot;
                    if (!weakSnapshot.TryGetTarget(out snapshot))
                    {
                        continue;
                    }

                    snapshot.StopTracking();
                }
            }
        }
    }
}
