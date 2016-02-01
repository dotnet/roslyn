// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// Base implementation of ITableDataSource
    /// </summary>
    internal abstract class AbstractTableDataSource<TData> : ITableDataSource
    {
        private readonly object _gate;

        // This map holds aggregation key to factory
        // Any data that shares same aggregation key will de-duplicated to same factory
        private readonly Dictionary<object, TableEntriesFactory<TData>> _map;

        // This map holds each data source key to its aggregation key
        private readonly Dictionary<object, object> _aggregateKeyMap;

        private ImmutableArray<SubscriptionWithoutLock> _subscriptions;
        protected bool IsStable;

        public AbstractTableDataSource(Workspace workspace)
        {
            _gate = new object();
            _map = new Dictionary<object, TableEntriesFactory<TData>>();
            _aggregateKeyMap = new Dictionary<object, object>();

            _subscriptions = ImmutableArray<SubscriptionWithoutLock>.Empty;

            Workspace = workspace;
            IsStable = true;
        }

        public Workspace Workspace { get; }

        public abstract string DisplayName { get; }

        public abstract string SourceTypeIdentifier { get; }

        public abstract string Identifier { get; }

        public void RefreshAllFactories()
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            List<TableEntriesFactory<TData>> factories;
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

        public void Refresh(TableEntriesFactory<TData> factory)
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

        public abstract ImmutableArray<TableItem<TData>> Deduplicate(IEnumerable<IList<TableItem<TData>>> duplicatedGroups);
        public abstract ITrackingPoint CreateTrackingPoint(TData data, ITextSnapshot snapshot);
        public abstract AbstractTableEntriesSnapshot<TData> CreateSnapshot(AbstractTableEntriesSource<TData> source, int version, ImmutableArray<TableItem<TData>> items, ImmutableArray<ITrackingPoint> trackingPoints);

        /// <summary>
        /// Get unique ID per given data such as DiagnosticUpdatedArgs or TodoUpdatedArgs.
        /// Data contains multiple items belong to one logical chunk. and the Id represents this particular 
        /// chunk of the data
        /// </summary>
        public abstract object GetItemKey(object data);

        /// <summary>
        /// Create TableEntriesSource for the given data.
        /// </summary>
        public abstract AbstractTableEntriesSource<TData> CreateTableEntriesSource(object data);

        /// <summary>
        /// Get unique ID for given data that will be used to find data whose items needed to be merged together.
        /// 
        /// for example, for linked files, data that belong to same physical file will be gathered and items that belong to
        /// those data will be de-duplicated.
        /// </summary>
        protected abstract object GetOrUpdateAggregationKey(object data);

        protected void OnDataAddedOrChanged(object data)
        {
            // reuse factory. it is okay to re-use factory since we make sure we remove the factory before
            // adding it back
            bool newFactory = false;
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            TableEntriesFactory<TData> factory;

            lock (_gate)
            {
                snapshot = _subscriptions;
                GetOrCreateFactory_NoLock(data, out factory, out newFactory);

                factory.OnDataAddedOrChanged(data);

                NotifySubscriptionOnDataAddedOrChanged_NoLock(snapshot, factory, newFactory);
            }
        }

        protected void OnDataRemoved(object data)
        {
            lock (_gate)
            {
                RemoveStaledData(data);
            }
        }

        protected void RemoveStaledData(object data)
        {
            OnDataRemoved_NoLock(data);

            RemoveAggregateKey_NoLock(data);
        }

        private void OnDataRemoved_NoLock(object data)
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            TableEntriesFactory<TData> factory;

            var key = TryGetAggregateKey(data);
            if (key == null)
            {
                // never created before.
                return;
            }

            snapshot = _subscriptions;
            if (!_map.TryGetValue(key, out factory))
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

        private static void NotifySubscriptionOnDataAddedOrChanged_NoLock(ImmutableArray<SubscriptionWithoutLock> snapshot, TableEntriesFactory<TData> factory, bool newFactory)
        {
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory);
            }
        }

        private static void NotifySubscriptionOnDataRemoved_NoLock(ImmutableArray<SubscriptionWithoutLock> snapshot, TableEntriesFactory<TData> factory)
        {
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Remove(factory);
            }
        }

        private void GetOrCreateFactory_NoLock(object data, out TableEntriesFactory<TData> factory, out bool newFactory)
        {
            newFactory = false;

            var key = GetOrUpdateAggregationKey(data);
            if (_map.TryGetValue(key, out factory))
            {
                return;
            }

            var source = CreateTableEntriesSource(data);
            factory = new TableEntriesFactory<TData>(this, source);

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

        protected void AddAggregateKey(object data, object aggregateKey)
        {
            _aggregateKeyMap.Add(GetItemKey(data), aggregateKey);
        }

        protected object TryGetAggregateKey(object data)
        {
            object aggregateKey;
            var key = GetItemKey(data);
            if (_aggregateKeyMap.TryGetValue(key, out aggregateKey))
            {
                return aggregateKey;
            }

            return null;
        }

        private void RemoveAggregateKey_NoLock(object data)
        {
            _aggregateKeyMap.Remove(GetItemKey(data));
        }

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
            private readonly AbstractTableDataSource<TData> _source;
            private readonly ITableDataSink _sink;

            public SubscriptionWithoutLock(AbstractTableDataSource<TData> source, ITableDataSink sink)
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
            {
                _sink.RemoveFactory(factory);
            }

            public void RemoveAll()
            {
                _sink.RemoveAllFactories();
            }

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
            {
                UpdateSubscriptions(s => s.Add(this));
            }

            private void UnRegister()
            {
                UpdateSubscriptions(s => s.Remove(this));
            }

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
