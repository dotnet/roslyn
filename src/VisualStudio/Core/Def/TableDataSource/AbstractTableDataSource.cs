// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Base implementation of ITableDataSource
    /// </summary>
    internal abstract class AbstractTableDataSource<TItem, TData> : ITableDataSource
        where TItem : TableItem
        where TData : notnull
    {
        private readonly object _gate;

        // This map holds aggregation key to factory
        // Any data that shares same aggregation key will de-duplicated to same factory
        private readonly Dictionary<object, TableEntriesFactory<TItem, TData>> _map;

        // This map holds each data source key to its aggregation key
        private readonly Dictionary<object, object> _aggregateKeyMap;

        private ImmutableArray<SubscriptionWithoutLock> _subscriptions;
        protected bool IsStable;

        public AbstractTableDataSource(Workspace workspace, IThreadingContext threadingContext)
        {
            _gate = new object();
            _map = new Dictionary<object, TableEntriesFactory<TItem, TData>>();
            _aggregateKeyMap = new Dictionary<object, object>();

            _subscriptions = ImmutableArray<SubscriptionWithoutLock>.Empty;

            Workspace = workspace;
            ThreadingContext = threadingContext;
            IsStable = true;
        }

        public Workspace Workspace { get; }

        public abstract string DisplayName { get; }

        public abstract string SourceTypeIdentifier { get; }

        public abstract string Identifier { get; }

        protected IThreadingContext ThreadingContext { get; }

        public void RefreshAllFactories()
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            List<TableEntriesFactory<TItem, TData>> factories;
            lock (_gate)
            {
                snapshot = _subscriptions;
                factories = _map.Values.ToList();
            }

            // let table manager know that we want to refresh factories.
            for (var i = 0; i < snapshot.Length; i++)
            {
                foreach (var factory in factories)
                {
                    factory.OnRefreshed();

                    snapshot[i].AddOrUpdate(factory, newFactory: false);
                }
            }
        }

        public void Refresh(TableEntriesFactory<TItem, TData> factory)
        {
            var snapshot = _subscriptions;

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory: false);
            }
        }

        public void Shutdown()
        {
            // editor team wants us to update snapshot versions before
            // removing factories on shutdown.
            RefreshAllFactories();

            // and then remove all factories.
            ImmutableArray<SubscriptionWithoutLock> snapshot;

            lock (_gate)
            {
                snapshot = _subscriptions;
                _map.Clear();
            }

            // let table manager know that we want to clear all factories
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].RemoveAll();
            }
        }

        public ImmutableArray<TItem> AggregateItems<TKey>(IEnumerable<IGrouping<TKey, TItem>> groupedItems)
        {
            using var _0 = ArrayBuilder<TItem>.GetInstance(out var aggregateItems);
            using var _1 = ArrayBuilder<string>.GetInstance(out var projectNames);
            using var _2 = ArrayBuilder<Guid>.GetInstance(out var projectGuids);

            string[]? stringArrayCache = null;
            Guid[]? guidArrayCache = null;

            static T[] GetOrCreateArray<T>([NotNull] ref T[]? cache, ArrayBuilder<T> value)
                => (cache != null && Enumerable.SequenceEqual(cache, value)) ? cache : (cache = value.ToArray());

            foreach (var (_, items) in groupedItems)
            {
                TItem? firstItem = null;
                var hasSingle = true;

                foreach (var item in items)
                {
                    if (firstItem == null)
                    {
                        firstItem = item;
                    }
                    else
                    {
                        hasSingle = false;
                    }

                    if (item.ProjectName != null)
                    {
                        projectNames.Add(item.ProjectName);
                    }

                    if (item.ProjectGuid != Guid.Empty)
                    {
                        projectGuids.Add(item.ProjectGuid);
                    }
                }

                // firstItem could only be null if a group was empty, which is not allowed
                RoslynDebug.AssertNotNull(firstItem);

                if (hasSingle)
                {
                    aggregateItems.Add(firstItem);
                }
                else
                {
                    projectNames.SortAndRemoveDuplicates(StringComparer.Ordinal);
                    projectGuids.SortAndRemoveDuplicates(Comparer<Guid>.Default);

                    aggregateItems.Add((TItem)firstItem.WithAggregatedData(GetOrCreateArray(ref stringArrayCache, projectNames), GetOrCreateArray(ref guidArrayCache, projectGuids)));
                }

                projectNames.Clear();
                projectGuids.Clear();
            }

            return Order(aggregateItems).ToImmutableArray();
        }

        public abstract IEqualityComparer<TItem> GroupingComparer { get; }

        public abstract IEnumerable<TItem> Order(IEnumerable<TItem> groupedItems);

        public abstract AbstractTableEntriesSnapshot<TItem> CreateSnapshot(AbstractTableEntriesSource<TItem> source, int version, ImmutableArray<TItem> items, ImmutableArray<ITrackingPoint> trackingPoints);

        /// <summary>
        /// Get unique ID per given data such as DiagnosticUpdatedArgs or TodoUpdatedArgs.
        /// Data contains multiple items belong to one logical chunk. and the Id represents this particular 
        /// chunk of the data
        /// </summary>
        public abstract object GetItemKey(TData data);

        /// <summary>
        /// Create TableEntriesSource for the given data.
        /// </summary>
        public abstract AbstractTableEntriesSource<TItem> CreateTableEntriesSource(object data);

        /// <summary>
        /// Get unique ID for given data that will be used to find data whose items needed to be merged together.
        /// 
        /// for example, for linked files, data that belong to same physical file will be gathered and items that belong to
        /// those data will be de-duplicated.
        /// </summary>
        protected abstract object GetOrUpdateAggregationKey(TData data);

        protected void OnDataAddedOrChanged(TData data)
        {
            // reuse factory. it is okay to re-use factory since we make sure we remove the factory before
            // adding it back
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            lock (_gate)
            {
                snapshot = _subscriptions;
                GetOrCreateFactory_NoLock(data, out var factory, out var newFactory);

                factory.OnDataAddedOrChanged(data);

                NotifySubscriptionOnDataAddedOrChanged_NoLock(snapshot, factory, newFactory);
            }
        }

        protected void OnDataRemoved(TData data)
        {
            lock (_gate)
            {
                RemoveStaledData(data);
            }
        }

        protected void RemoveStaledData(TData data)
        {
            OnDataRemoved_NoLock(data);

            RemoveAggregateKey_NoLock(data);
        }

        private void OnDataRemoved_NoLock(TData data)
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            var key = TryGetAggregateKey(data);
            if (key == null)
            {
                // never created before.
                return;
            }

            snapshot = _subscriptions;
            if (!_map.TryGetValue(key, out var factory))
            {
                // never reported about this before
                return;
            }

            // remove this particular item from map
            if (!factory.OnDataRemoved(data))
            {
                // let error list know that factory has changed.
                NotifySubscriptionOnDataAddedOrChanged_NoLock(snapshot, factory, newFactory: false);
                return;
            }

            // everything belong to the factory has removed. remove the factory
            _map.Remove(key);

            // let table manager know that we want to clear the entries
            NotifySubscriptionOnDataRemoved_NoLock(snapshot, factory);
        }

        private static void NotifySubscriptionOnDataAddedOrChanged_NoLock(ImmutableArray<SubscriptionWithoutLock> snapshot, TableEntriesFactory<TItem, TData> factory, bool newFactory)
        {
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory);
            }
        }

        private static void NotifySubscriptionOnDataRemoved_NoLock(ImmutableArray<SubscriptionWithoutLock> snapshot, TableEntriesFactory<TItem, TData> factory)
        {
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Remove(factory);
            }
        }

        private void GetOrCreateFactory_NoLock(TData data, out TableEntriesFactory<TItem, TData> factory, out bool newFactory)
        {
            newFactory = false;

            var key = GetOrUpdateAggregationKey(data);
            if (_map.TryGetValue(key, out factory))
            {
                return;
            }

            var source = CreateTableEntriesSource(data);
            factory = new TableEntriesFactory<TItem, TData>(ThreadingContext, this, source);

            _map.Add(key, factory);
            newFactory = true;
        }

        protected void ChangeStableState(bool stable)
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;

            lock (_gate)
            {
                snapshot = _subscriptions;
            }

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].IsStable = stable;
            }
        }

        protected void AddAggregateKey(TData data, object aggregateKey)
            => _aggregateKeyMap.Add(GetItemKey(data), aggregateKey);

        protected object? TryGetAggregateKey(TData data)
        {
            var key = GetItemKey(data);
            if (_aggregateKeyMap.TryGetValue(key, out var aggregateKey))
            {
                return aggregateKey;
            }

            return null;
        }

        private void RemoveAggregateKey_NoLock(TData data)
            => _aggregateKeyMap.Remove(GetItemKey(data));

        IDisposable ITableDataSource.Subscribe(ITableDataSink sink)
        {
            lock (_gate)
            {
                return new SubscriptionWithoutLock(this, sink);
            }
        }

        internal int NumberOfSubscription_TestOnly
        {
            get { return _subscriptions.Length; }
        }

        protected class SubscriptionWithoutLock : IDisposable
        {
            private readonly AbstractTableDataSource<TItem, TData> _source;
            private readonly ITableDataSink _sink;

            public SubscriptionWithoutLock(AbstractTableDataSource<TItem, TData> source, ITableDataSink sink)
            {
                _source = source;
                _sink = sink;

                Register();
                ReportInitialData();
            }

            public bool IsStable
            {
                get
                {
                    return _sink.IsStable;
                }

                set
                {
                    _sink.IsStable = value;
                }
            }

            public void AddOrUpdate(ITableEntriesSnapshotFactory provider, bool newFactory)
            {
                if (newFactory)
                {
                    _sink.AddFactory(provider);
                    return;
                }

                _sink.FactorySnapshotChanged(provider);
            }

            public void Remove(ITableEntriesSnapshotFactory factory)
                => _sink.RemoveFactory(factory);

            public void RemoveAll()
                => _sink.RemoveAllFactories();

            public void Dispose()
            {
                // REVIEW: will closing task hub dispose this subscription?
                UnRegister();
            }

            private void ReportInitialData()
            {
                foreach (var provider in _source._map.Values)
                {
                    AddOrUpdate(provider, newFactory: true);
                }

                IsStable = _source.IsStable;
            }

            private void Register()
                => UpdateSubscriptions(s => s.Add(this));

            private void UnRegister()
                => UpdateSubscriptions(s => s.Remove(this));

            private void UpdateSubscriptions(Func<ImmutableArray<SubscriptionWithoutLock>, ImmutableArray<SubscriptionWithoutLock>> update)
            {
                while (true)
                {
                    var current = _source._subscriptions;
                    var @new = update(current);

                    // try replace with new list
                    var registered = ImmutableInterlocked.InterlockedCompareExchange(ref _source._subscriptions, @new, current);
                    if (registered == current)
                    {
                        // succeeded
                        break;
                    }
                }
            }
        }
    }
}
