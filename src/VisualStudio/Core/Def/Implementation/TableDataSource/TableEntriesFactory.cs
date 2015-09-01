// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class TableEntriesFactory<TData> : ITableEntriesSnapshotFactory
    {
        private readonly object _gate = new object();

        private readonly AbstractTableDataSource<TData> _source;
        private readonly AggregatedEntriesSource _entriesSources;
        private readonly WeakReference<ITableEntriesSnapshot> _lastSnapshotWeakReference = new WeakReference<ITableEntriesSnapshot>(null);

        private int _lastVersion = 0;

        public TableEntriesFactory(AbstractTableDataSource<TData> source, AbstractTableEntriesSource<TData> entriesSource)
        {
            _source = source;
            _entriesSources = new AggregatedEntriesSource(entriesSource);
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
                    _source.Refresh(this);
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

                _entriesSources.OnDataAddedOrChanged(data, _source);
            }
        }

        public bool OnDataRemoved(object data)
        {
            lock (_gate)
            {
                UpdateVersion_NoLock();

                return _entriesSources.OnDataRemoved(data, _source);
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

        private ITableEntriesSnapshot CreateSnapshot(int version, ImmutableArray<TData> items)
        {
            var snapshot = _entriesSources.CreateSnapshot(version, items, _entriesSources.GetTrackingPoints(items));
            _lastSnapshotWeakReference.SetTarget(snapshot);

            return snapshot;
        }

        private class AggregatedEntriesSource
        {
            private readonly SourceCollections _sources;

            public AggregatedEntriesSource(AbstractTableEntriesSource<TData> primary)
            {
                _sources = new SourceCollections(primary);
            }

            public void OnDataAddedOrChanged(object data, AbstractTableDataSource<TData> source)
            {
                _sources.OnDataAddedOrChanged(data, source);
            }

            public bool OnDataRemoved(object data, AbstractTableDataSource<TData> source)
            {
                return _sources.OnDataRemoved(data, source);
            }

            public ImmutableArray<TData> GetItems()
            {
                if (_sources.Primary != null)
                {
                    return _sources.Primary.GetItems();
                }

                return ImmutableArray<TData>.Empty;
            }

            public ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TData> items)
            {
                if (_sources.Primary != null)
                {
                    return _sources.Primary.GetTrackingPoints(items);
                }

                return ImmutableArray<ITrackingPoint>.Empty;
            }

            public AbstractTableEntriesSnapshot<TData> CreateSnapshot(int version, ImmutableArray<TData> items, ImmutableArray<ITrackingPoint> trackingPoints)
            {
                if (_sources.Primary != null)
                {
                    return _sources.Primary.CreateSnapshot(version, items, trackingPoints);
                }

                return null;
            }

            private struct SourceCollections
            {
                private AbstractTableEntriesSource<TData> _primary;
                private Dictionary<object, AbstractTableEntriesSource<TData>> _sources;

                public SourceCollections(AbstractTableEntriesSource<TData> primary) : this()
                {
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
                            return _sources[0];
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
                    if (_primary != null && _primary.Key == key)
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

                    var source = tableSource.CreateTableEntrySource(data);
                    _sources.Add(source.Key, source);
                }

                public bool OnDataRemoved(object data, AbstractTableDataSource<TData> tableSource)
                {
                    var key = tableSource.GetItemKey(data);
                    if (_primary != null && _primary.Key == key)
                    {
                        return true;
                    }

                    if (_sources != null)
                    {
                        _sources.Remove(key);
                        return _sources.Count == 0;
                    }

                    return false;
                }
            }
        }
    }
}
