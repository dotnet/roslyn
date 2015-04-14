// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TableControl;
using Microsoft.VisualStudio.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableEntriesFactory<TData> : ITableEntriesSnapshotFactory
    {
        private readonly object _gate = new object();
        private readonly AbstractTableDataSource<TData> _source;
        private readonly WeakReference<ITableEntriesSnapshot> _lastSnapshotWeakReference = new WeakReference<ITableEntriesSnapshot>(null);

        private int _lastVersion = 0;
        private int _lastItemCount = 0;

        public AbstractTableEntriesFactory(AbstractTableDataSource<TData> source)
        {
            _source = source;
        }

        protected abstract ImmutableArray<TData> GetItems();
        protected abstract ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TData> items);
        protected abstract AbstractTableEntriesSnapshot<TData> CreateSnapshot(int version, ImmutableArray<TData> items, ImmutableArray<ITrackingPoint> trackingPoints);

        public int CurrentVersionNumber
        {
            get
            {
                lock (_gate)
                {
                    return _lastVersion;
                }
            }
        }

        public ITableEntriesSnapshot GetCurrentSnapshot()
        {
            lock (_gate)
            {
                var version = _lastVersion;

                ITableEntriesSnapshot lastSnapshot;
                if (TryGetLastSnapshot(version, out lastSnapshot))
                {
                    return lastSnapshot;
                }

                var itemCount = _lastItemCount;
                var items = GetItems();

                if (items.Length != itemCount)
                {
                    _lastItemCount = items.Length;
                    _source.Refresh(this);
                }

                return CreateSnapshot(version, items);
            }
        }

        public ITableEntriesSnapshot GetSnapshot(int versionNumber)
        {
            lock (_gate)
            {
                ITableEntriesSnapshot lastSnapshot;
                if (TryGetLastSnapshot(versionNumber, out lastSnapshot))
                {
                    return lastSnapshot;
                }

                var version = _lastVersion;
                if (version != versionNumber)
                {
                    _source.Refresh(this);
                    return null;
                }

                // version between error list and diagnostic service is different. 
                // so even if our version is same, diagnostic service version might be different.
                //
                // this is a kind of sanity check to reduce number of times we return wrong snapshot.
                // but the issue will quickly fixed up since diagnostic service will drive error list to latest snapshot.
                var items = GetItems();
                if (items.Length != _lastItemCount)
                {
                    _source.Refresh(this);
                    return null;
                }

                return CreateSnapshot(version, items);
            }
        }

        public void OnUpdated(int count)
        {
            lock (_gate)
            {
                _lastVersion++;
                _lastItemCount = count;
            }
        }

        public void OnRefreshed()
        {
            lock (_gate)
            {
                _lastVersion++;
            }
        }

        public void Dispose()
        {
        }

        private bool TryGetLastSnapshot(int version, out ITableEntriesSnapshot lastSnapshot)
        {
            return _lastSnapshotWeakReference.TryGetTarget(out lastSnapshot) &&
                   lastSnapshot.VersionNumber == version;
        }

        private ITableEntriesSnapshot CreateSnapshot(int version, ImmutableArray<TData> items)
        {
            var snapshot = CreateSnapshot(version, items, GetTrackingPoints(items));
            _lastSnapshotWeakReference.SetTarget(snapshot);

            return snapshot;
        }

        protected ImmutableArray<ITrackingPoint> CreateTrackingPoints(
            Workspace workspace, DocumentId documentId,
            ImmutableArray<TData> items, Func<TData, ITextSnapshot, ITrackingPoint> converter)
        {
            if (documentId == null)
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var solution = workspace.CurrentSolution;
            var document = solution.GetDocument(documentId);
            if (document == null || !document.IsOpen())
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            SourceText text;
            if (!document.TryGetText(out text))
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot != null)
            {
                return items.Select(d => converter(d, snapshot)).ToImmutableArray();
            }

            var textBuffer = text.Container.TryGetTextBuffer();
            if (textBuffer == null)
            {
                return ImmutableArray<ITrackingPoint>.Empty;
            }

            var currentSnapshot = textBuffer.CurrentSnapshot;
            return items.Select(d => converter(d, currentSnapshot)).ToImmutableArray();
        }

        protected ITrackingPoint CreateTrackingPoint(ITextSnapshot snapshot, int line, int column)
        {
            if (snapshot.Length == 0)
            {
                return snapshot.CreateTrackingPoint(0, PointTrackingMode.Negative);
            }

            if (line >= snapshot.LineCount)
            {
                return snapshot.CreateTrackingPoint(snapshot.Length, PointTrackingMode.Positive);
            }

            var adjustedLine = Math.Max(line, 0);
            var textLine = snapshot.GetLineFromLineNumber(adjustedLine);
            if (column >= textLine.Length)
            {
                return snapshot.CreateTrackingPoint(textLine.End, PointTrackingMode.Positive);
            }

            var adjustedColumn = Math.Max(column, 0);
            return snapshot.CreateTrackingPoint(textLine.Start + adjustedColumn, PointTrackingMode.Positive);
        }
    }
}
