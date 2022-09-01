// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal class TableEntriesFactory<TItem, TData> : ITableEntriesSnapshotFactory
        where TItem : TableItem
        where TData : notnull
    {
        private readonly object _gate = new();

        private readonly AbstractTableDataSource<TItem, TData> _tableSource;
        private readonly AggregatedEntriesSource _entriesSources;
        private readonly WeakReference<ITableEntriesSnapshot> _lastSnapshotWeakReference = new(null!);

        private int _lastVersion = 0;

        public TableEntriesFactory(IThreadingContext threadingContext, AbstractTableDataSource<TItem, TData> tableSource, AbstractTableEntriesSource<TItem> entriesSource)
        {
            _tableSource = tableSource;
            _entriesSources = new AggregatedEntriesSource(threadingContext, _tableSource, entriesSource);
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
                if (TryGetLastSnapshot(version, out var lastSnapshot))
                {
                    return lastSnapshot;
                }

                var items = _entriesSources.GetItems();
                return CreateSnapshot(version, items);
            }
        }

        public ITableEntriesSnapshot? GetSnapshot(int versionNumber)
        {
            lock (_gate)
            {
                if (TryGetLastSnapshot(versionNumber, out var lastSnapshot))
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

        public void OnDataAddedOrChanged(TData data)
        {
            lock (_gate)
            {
                UpdateVersion_NoLock();

                _entriesSources.OnDataAddedOrChanged(data);
            }
        }

        public bool OnDataRemoved(TData data)
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
            => _lastVersion++;

        public void Dispose()
        {
        }

        private bool TryGetLastSnapshot(int version, out ITableEntriesSnapshot lastSnapshot)
        {
            return _lastSnapshotWeakReference.TryGetTarget(out lastSnapshot) &&
                   lastSnapshot.VersionNumber == version;
        }

        private ITableEntriesSnapshot CreateSnapshot(int version, ImmutableArray<TItem> items)
        {
            var snapshot = _entriesSources.CreateSnapshot(version, items, _entriesSources.GetTrackingPoints(items));
            _lastSnapshotWeakReference.SetTarget(snapshot);

            return snapshot;
        }

        private class AggregatedEntriesSource
        {
            private readonly IThreadingContext _threadingContext;
            private readonly EntriesSourceCollections _sources;
            private readonly AbstractTableDataSource<TItem, TData> _tableSource;

            public AggregatedEntriesSource(IThreadingContext threadingContext, AbstractTableDataSource<TItem, TData> tableSource, AbstractTableEntriesSource<TItem> primary)
            {
                _threadingContext = threadingContext;
                _tableSource = tableSource;
                _sources = new EntriesSourceCollections(primary);
            }

            public void OnDataAddedOrChanged(TData data)
                => _sources.OnDataAddedOrChanged(data, _tableSource);

            public bool OnDataRemoved(TData data)
                => _sources.OnDataRemoved(data, _tableSource);

            public ImmutableArray<TItem> GetItems()
            {
                if (_sources.Primary != null)
                {
                    return _sources.Primary.GetItems();
                }

                // flatten items from multiple sources and group them by deduplication identity
                // merge duplicated items into de-duplicated item list
                return _tableSource.AggregateItems(
                    _sources.GetSources()
                    .SelectMany(s => s.GetItems())
                    .GroupBy(d => d, _tableSource.GroupingComparer));
            }

            public ImmutableArray<ITrackingPoint> GetTrackingPoints(ImmutableArray<TItem> items)
            {
                if (items.Length == 0)
                {
                    return ImmutableArray<ITrackingPoint>.Empty;
                }

                if (_sources.Primary != null)
                {
                    return _sources.Primary.GetTrackingPoints(items);
                }

                return _tableSource.Workspace.CreateTrackingPoints(items[0].DocumentId, items);
            }

            public AbstractTableEntriesSnapshot<TItem> CreateSnapshot(int version, ImmutableArray<TItem> items, ImmutableArray<ITrackingPoint> trackingPoints)
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
                    return new EmptySnapshot(_threadingContext, version);
                }

                return _tableSource.CreateSnapshot(source, version, items, trackingPoints);
            }

            private class EmptySnapshot : AbstractTableEntriesSnapshot<TItem>
            {
                public EmptySnapshot(IThreadingContext threadingContext, int version)
                    : base(threadingContext, version, ImmutableArray<TItem>.Empty, ImmutableArray<ITrackingPoint>.Empty)
                {
                }

                public override bool TryNavigateTo(int index, NavigationOptions options, CancellationToken cancellationToken) => false;

                public override bool TryGetValue(int index, string columnName, [NotNullWhen(true)] out object? content)
                {
                    content = null;
                    return false;
                }
            }

            private class EntriesSourceCollections
            {
                private AbstractTableEntriesSource<TItem>? _primary;
                private Dictionary<object, AbstractTableEntriesSource<TItem>>? _sources;

                public EntriesSourceCollections(AbstractTableEntriesSource<TItem> primary)
                {
                    Contract.ThrowIfNull(primary);
                    _primary = primary;
                }

                public AbstractTableEntriesSource<TItem>? Primary
                {
                    get
                    {
                        if (_primary != null)
                        {
                            return _primary;
                        }

                        RoslynDebug.AssertNotNull(_sources);
                        if (_sources.Count == 1)
                        {
                            return _sources.Values.First();
                        }

                        return null;
                    }
                }

                public IEnumerable<AbstractTableEntriesSource<TItem>> GetSources()
                {
                    EnsureSources();
                    return _sources.Values;
                }

                [MemberNotNull(nameof(_sources))]
                private void EnsureSources()
                {
                    if (_sources == null)
                    {
                        RoslynDebug.AssertNotNull(_primary);
                        _sources = new Dictionary<object, AbstractTableEntriesSource<TItem>>
                        {
                            { _primary.Key, _primary }
                        };
                        _primary = null;
                    }
                }

                public void OnDataAddedOrChanged(TData data, AbstractTableDataSource<TItem, TData> tableSource)
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

                public bool OnDataRemoved(TData data, AbstractTableDataSource<TItem, TData> tableSource)
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
