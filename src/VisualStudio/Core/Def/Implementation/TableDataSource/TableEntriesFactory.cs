// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class TableEntriesFactory<TData> : ITableEntriesSnapshotFactory
    {
        private readonly object _gate = new object();

        private readonly AbstractTableDataSource<TData> _tableSource;
        private readonly AggregatedEntriesSource _entriesSources;
        private readonly WeakReference<ITableEntriesSnapshot> _lastSnapshotWeakReference = new WeakReference<ITableEntriesSnapshot>(null);

        private int _lastVersion = 0;

        public TableEntriesFactory(AbstractTableDataSource<TData> tableSource, AbstractTableEntriesSource<TData> entriesSource)
        {
            _tableSource = tableSource;
            _entriesSources = new AggregatedEntriesSource(_tableSource, entriesSource);
        }

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

                var items = _entriesSources.GetItems();
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
                    _tableSource.Refresh(this);
                    return null;
                }

                var items = _entriesSources.GetItems();
                return CreateSnapshot(version, items);
            }
        }

        public void OnDataAddedOrChanged(object data)
        {
            lock (_gate)
            {
                UpdateVersion_NoLock();

                _entriesSources.OnDataAddedOrChanged(data);
            }
        }

        public bool OnDataRemoved(object data)
        {
            lock (_gate)
            {
                UpdateVersion_NoLock();
                return _entriesSources.OnDataRemoved(data);
            }
        }

        public void OnRefreshed()
        {
            lock (_gate)
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

        private ITableEntriesSnapshot CreateSnapshot(int version, ImmutableArray<TableItem<TData>> items)
        {
            var snapshot = _entriesSources.CreateSnapshot(version, items, _entriesSources.GetTrackingPoints(items));
            _lastSnapshotWeakReference.SetTarget(snapshot);

            return snapshot;
        }

        private class AggregatedEntriesSource
        {
            private readonly EntriesSourceCollections _sources;
            private readonly AbstractTableDataSource<TData> _tableSource;

            public AggregatedEntriesSource(AbstractTableDataSource<TData> tableSource, AbstractTableEntriesSource<TData> primary)
            {
                _tableSource = tableSource;
                _sources = new EntriesSourceCollections(primary);
            }

            public void OnDataAddedOrChanged(object data)
            {
                _sources.OnDataAddedOrChanged(data, _tableSource);
            }

            public bool OnDataRemoved(object data)
            {
                return _sources.OnDataRemoved(data, _tableSource);
            }

            public ImmutableArray<TableItem<TData>> GetItems()
            {
                if (_sources.Primary != null)
                {
                    return _sources.Primary.GetItems();
                }

                // flatten items from multiple sources and group them by deduplication identity
                // merge duplicated items into de-duplicated item list
                var items = _sources.GetSources()
                                    .SelectMany(s => s.GetItems())
                                    .GroupBy(d => d.DeduplicationKey)
                                    .Select(g => (IList<TableItem<TData>>)g);

                return _tableSource.Deduplicate(items);
            }

            public ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TableItem<TData>> items)
            {
                if (items.Length == 0)
                {
                    return ImmutableArray<ITrackingPoint>.Empty;
                }

                if (_sources.Primary != null)
                {
                    return _sources.Primary.GetTrackingPoints(items);
                }

                return _tableSource.Workspace.CreateTrackingPoints(items[0].PrimaryDocumentId, items, (d, s) => _tableSource.CreateTrackingPoint(d, s));
            }

            public AbstractTableEntriesSnapshot<TData> CreateSnapshot(int version, ImmutableArray<TableItem<TData>> items, ImmutableArray<ITrackingPoint> trackingPoints)
            {
                if (_sources.Primary != null)
                {
                    return _tableSource.CreateSnapshot(_sources.Primary, version, items, trackingPoints);
                }

                // we can be called back from error list while all sources are removed but before error list know about it yet 
                // since notification is pending in the queue.
                var source = _sources.GetSources().FirstOrDefault();
                if (source == null)
                {
                    return new EmptySnapshot(version);
                }

                return _tableSource.CreateSnapshot(source, version, items, trackingPoints);
            }

            private class EmptySnapshot : AbstractTableEntriesSnapshot<TData>
            {
                public EmptySnapshot(int version) :
                    base(version, ImmutableArray<TableItem<TData>>.Empty, ImmutableArray<ITrackingPoint>.Empty)
                {
                }

                protected override bool IsEquivalent(TData item1, TData item2) => false;
                public override bool TryNavigateTo(int index, bool previewTab) => false;

                public override bool TryGetValue(int index, string columnName, out object content)
                {
                    content = null;
                    return false;
                }
            }

            private class EntriesSourceCollections
            {
                private AbstractTableEntriesSource<TData> _primary;
                private Dictionary<object, AbstractTableEntriesSource<TData>> _sources;

                public EntriesSourceCollections(AbstractTableEntriesSource<TData> primary)
                {
                    Contract.ThrowIfNull(primary);
                    _primary = primary;
                }

                public AbstractTableEntriesSource<TData> Primary
                {
                    get
                    {
                        if (_primary != null)
                        {
                            return _primary;
                        }

                        if (_sources.Count == 1)
                        {
                            return _sources.Values.First();
                        }

                        return null;
                    }
                }

                public IEnumerable<AbstractTableEntriesSource<TData>> GetSources()
                {
                    EnsureSources();
                    return _sources.Values;
                }

                private void EnsureSources()
                {
                    if (_sources == null)
                    {
                        _sources = new Dictionary<object, AbstractTableEntriesSource<TData>>();
                        _sources.Add(_primary.Key, _primary);
                        _primary = null;
                    }
                }

                public void OnDataAddedOrChanged(object data, AbstractTableDataSource<TData> tableSource)
                {
                    var key = tableSource.GetItemKey(data);
                    if (_primary != null && _primary.Key.Equals(key))
                    {
                        return;
                    }

                    if (_sources != null)
                    {
                        if (_sources.ContainsKey(key))
                        {
                            return;
                        }
                    }

                    EnsureSources();

                    var source = tableSource.CreateTableEntriesSource(data);
                    _sources.Add(source.Key, source);
                }

                public bool OnDataRemoved(object data, AbstractTableDataSource<TData> tableSource)
                {
                    var key = tableSource.GetItemKey(data);
                    if (_primary != null && _primary.Key.Equals(key))
                    {
                        return true;
                    }

                    if (_sources != null)
                    {
                        _sources.Remove(key);
                        return _sources.Count == 0;
                    }

                    // they never reported to us before
                    return false;
                }
            }
        }
    }
}
