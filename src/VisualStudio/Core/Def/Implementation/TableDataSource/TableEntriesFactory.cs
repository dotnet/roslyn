// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class TableEntriesFactory<TData> : ITableEntriesSnapshotFactory
    {
        private readonly AbstractTableDataSource<TData> _source;
        private readonly AbstractTableEntriesSource<TData> _entriesSource;
        private readonly WeakReference<ITableEntriesSnapshot> _lastSnapshotWeakReference = new WeakReference<ITableEntriesSnapshot>(null);

        private int _lastVersion = 0;
        private int _lastItemCount = 0;

        protected readonly object Gate = new object();

        public TableEntriesFactory(AbstractTableDataSource<TData> source, AbstractTableEntriesSource<TData> entriesSource)
        {
            _source = source;
            _entriesSource = entriesSource;
        }

        public int CurrentVersionNumber
        {
            get
            {
                lock (Gate)
                {
                    return _lastVersion;
                }
            }
        }

        public ITableEntriesSnapshot GetCurrentSnapshot()
        {
            lock (Gate)
            {
                var version = _lastVersion;

                ITableEntriesSnapshot lastSnapshot;
                if (TryGetLastSnapshot(version, out lastSnapshot))
                {
                    return lastSnapshot;
                }

                var itemCount = _lastItemCount;
                var items = _entriesSource.GetItems();

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
            lock (Gate)
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
                var items = _entriesSource.GetItems();
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
            lock (Gate)
            {
                UpdateVersion_NoLock();
                _lastItemCount = count;
            }
        }

        public void OnRefreshed()
        {
            lock (Gate)
            {
                UpdateVersion_NoLock();
            }
        }

        protected void UpdateVersion_NoLock()
        {
            _lastVersion++;
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
            var snapshot = _entriesSource.CreateSnapshot(version, items, _entriesSource.GetTrackingPoints(items));
            _lastSnapshotWeakReference.SetTarget(snapshot);

            return snapshot;
        }
    }
}
