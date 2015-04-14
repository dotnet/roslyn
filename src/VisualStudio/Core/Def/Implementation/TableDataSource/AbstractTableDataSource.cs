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
    internal abstract class AbstractTableDataSource<TData> : ITableDataSource
    {
        protected readonly object Gate;
        protected readonly Dictionary<object, AbstractTableEntriesFactory<TData>> Map;

        protected ImmutableArray<SubscriptionWithoutLock> Subscriptions;

        public AbstractTableDataSource()
        {
            Gate = new object();
            Map = new Dictionary<object, AbstractTableEntriesFactory<TData>>();
            Subscriptions = ImmutableArray<SubscriptionWithoutLock>.Empty;
        }

        public virtual void OnProjectDependencyChanged(Solution solution)
        {
            // base implementation does nothing.
        }

        public abstract string DisplayName { get; }

        public abstract Guid SourceTypeIdentifier { get; }

        public abstract Guid Identifier { get; }

        public void Refresh(AbstractTableEntriesFactory<TData> factory)
        {
            var snapshot = this.Subscriptions;

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory: false);
            }
        }

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

        protected void OnDataRemoved(object key)
        {
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            AbstractTableEntriesFactory<TData> factory;

            lock (Gate)
            {
                snapshot = Subscriptions;
                if (!Map.TryGetValue(key, out factory))
                {
                    // never reported about this before
                    return;
                }

                // remove it from map
                Map.Remove(key);
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

            lock (Gate)
            {
                snapshot = Subscriptions;
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

            lock (Gate)
            {
                snapshot = Subscriptions;
                factories = Map.Values.ToList();
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
            lock (Gate)
            {
                return new SubscriptionWithoutLock(this, sink);
            }
        }

        internal int NumberOfSubscription_TestOnly
        {
            get { return Subscriptions.Length; }
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
                foreach (var provider in _source.Map.Values)
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
                    var current = _source.Subscriptions;
                    var @new = update(current);

                    // try replace with new list
                    var registered = ImmutableInterlocked.InterlockedCompareExchange(ref _source.Subscriptions, @new, current);
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
