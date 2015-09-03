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
    internal abstract class AbstractTableDataSource<TData> : ITableDataSource
    {
        private readonly object _gate;
        private readonly Dictionary<object, TableEntriesFactory<TData>> _map;

        private ImmutableArray<SubscriptionWithoutLock> _subscriptions;
        protected bool IsStable;

        public AbstractTableDataSource(Workspace workspace)
        {
            _gate = new object();
            _map = new Dictionary<object, TableEntriesFactory<TData>>();
            _subscriptions = ImmutableArray<SubscriptionWithoutLock>.Empty;

            Workspace = workspace;
            IsStable = true;
        }

        public Workspace Workspace { get; }

        public abstract string DisplayName { get; }

        public abstract string SourceTypeIdentifier { get; }

        public abstract string Identifier { get; }

        public void Refresh(TableEntriesFactory<TData> factory)
        {
            var snapshot = this._subscriptions;

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory: false);
            }
        }

        public void Shutdown()
        {
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

        public abstract object GetItemKey(object data);
        public abstract AbstractTableEntriesSource<TData> CreateTableEntrySource(object data);
        public abstract ImmutableArray<TableItem<TData>> Deduplicate(IEnumerable<IList<TableItem<TData>>> duplicatedGroups);
        public abstract ITrackingPoint CreateTrackingPoint(TData data, ITextSnapshot snapshot);

        protected abstract object GetAggregationKey(object data);

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
                GetOrCreateFactory(data, out factory, out newFactory);

                factory.OnDataAddedOrChanged(data);
            }

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory);
            }
        }

        protected void OnDataRemoved(object data)
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            TableEntriesFactory<TData> factory;

            var key = GetAggregationKey(data);
            lock (_gate)
            {
                snapshot = _subscriptions;
                if (!_map.TryGetValue(key, out factory))
                {
                    // never reported about this before
                    return;
                }

                // remove it from map
                if (factory.OnDataRemoved(data))
                {
                    _map.Remove(key);
                }
            }

            // let table manager know that we want to clear the entries
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Remove(factory);
            }
        }

        private void GetOrCreateFactory(object data, out TableEntriesFactory<TData> factory, out bool newFactory)
        {
            var key = GetAggregationKey(data);

            newFactory = false;
            if (_map.TryGetValue(key, out factory))
            {
                return;
            }

            var source = CreateTableEntrySource(data);
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

        protected void RefreshAllFactories()
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
