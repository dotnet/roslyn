// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class OpenDocumentTracker
    {
        private readonly object _gate = new object();
        private readonly Dictionary<DocumentId, Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>>>> _map =
            new Dictionary<DocumentId, Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>>>>();

        private readonly Workspace _workspace;

        public OpenDocumentTracker(Workspace workspace)
        {
            _workspace = workspace;

            _workspace.DocumentClosed += OnDocumentClosed;
        }

        public void TrackOpenDocument(DocumentId documentId, object id, AbstractTableEntriesSnapshot<DiagnosticData> snapshot)
        {
            lock (_gate)
            {
                Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>>> secondMap;
                if (!_map.TryGetValue(documentId, out secondMap))
                {
                    secondMap = new Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>>>();
                    _map.Add(documentId, secondMap);
                }

                AbstractTableEntriesSnapshot<DiagnosticData> oldSnapshot;
                WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>> oldWeakSnapshot;
                if (secondMap.TryGetValue(id, out oldWeakSnapshot) && oldWeakSnapshot.TryGetTarget(out oldSnapshot))
                {
                    oldSnapshot.StopTracking();
                }

                secondMap[id] = new WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>>(snapshot);
            }
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs e)
        {
            lock (_gate)
            {
                Dictionary<object, WeakReference<AbstractTableEntriesSnapshot<DiagnosticData>>> secondMap;
                if (!_map.TryGetValue(e.Document.Id, out secondMap))
                {
                    return;
                }

                _map.Remove(e.Document.Id);
                foreach (var weakSnapshot in secondMap.Values)
                {
                    AbstractTableEntriesSnapshot<DiagnosticData> snapshot;
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
