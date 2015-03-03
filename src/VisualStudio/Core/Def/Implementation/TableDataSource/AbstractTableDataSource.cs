// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractTableDataSource<TArgs, TData> : ITableDataSource
    {
        private readonly object _gate;
        private readonly Dictionary<object, AbstractTableEntriesFactory<TData>> _map;

        private ImmutableArray<SubscriptionWithoutLock> _subscriptions;

        public AbstractTableDataSource()
        {
            _gate = new object();
            _map = new Dictionary<object, AbstractTableEntriesFactory<TData>>();
            _subscriptions = ImmutableArray<SubscriptionWithoutLock>.Empty;
        }

        public virtual void OnProjectDependencyChanged(Solution solution)
        {
            // base implementation does nothing.
        }

        public abstract string DisplayName { get; }

        public abstract Guid SourceTypeIdentifier { get; }

        public abstract Guid Identifier { get; }

        protected abstract AbstractTableEntriesFactory<TData> CreateTableEntryFactory(object key, TArgs data);

        protected void ConnectToSolutionCrawlerService(Workspace workspace)
        {
            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            var reporter = crawlerService.GetProgressReporter(workspace);

            // set initial value
            ChangeStableState(stable: !reporter.InProgress);

            reporter.Started += OnSolutionCrawlerStarted;
            reporter.Stopped += OnSolutionCrawlerStopped;
        }

        private void OnSolutionCrawlerStarted(object sender, EventArgs e)
        {
            ChangeStableState(stable: false);
        }

        private void OnSolutionCrawlerStopped(object sender, EventArgs e)
        {
            ChangeStableState(stable: true);
        }

        protected void OnDataAddedOrChanged(object key, TArgs data, int itemCount)
        {
            // reuse factory. it is okay to re-use factory since we make sure we remove the factory before
            // adding it back
            bool newFactory = false;
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            AbstractTableEntriesFactory<TData> factory;

            lock (_gate)
            {
                snapshot = _subscriptions;
                if (!_map.TryGetValue(key, out factory))
                {
                    factory = CreateTableEntryFactory(key, data);
                    _map.Add(key, factory);
                    newFactory = true;
                }
            }

            factory.OnUpdated(itemCount);

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory);
            }
        }

        protected void OnDataRemoved(object key)
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            AbstractTableEntriesFactory<TData> factory;

            lock (_gate)
            {
                snapshot = _subscriptions;
                if (!_map.TryGetValue(key, out factory))
                {
                    // never reported about this before
                    return;
                }

                // remove it from map
                _map.Remove(key);
            }

            factory.OnUpdated(0);

            // let table manager know that we want to clear the entries
            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].Remove(factory);
            }
        }

        private void ChangeStableState(bool stable)
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
            List<AbstractTableEntriesFactory<TData>> factories;

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

        private class SubscriptionWithoutLock : IDisposable
        {
            private readonly AbstractTableDataSource<TArgs, TData> _source;
            private readonly ITableDataSink _sink;

            public SubscriptionWithoutLock(AbstractTableDataSource<TArgs, TData> source, ITableDataSink sink)
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

                _sink.FactoryUpdated(provider);
            }

            public void Remove(ITableEntriesSnapshotFactory factory)
            {
                _sink.RemoveFactory(factory);
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
