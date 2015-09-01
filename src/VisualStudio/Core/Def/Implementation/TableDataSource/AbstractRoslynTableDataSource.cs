// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractRoslynTableDataSource<TArgs, TData> : AbstractTableDataSource<TData>
    {
        protected abstract AbstractTableEntriesSource<TData> CreateTableEntrySource(object key, TArgs data);
        protected abstract object GetKey(object key, TArgs data);

        protected void OnDataAddedOrChanged(Solution solution, ProjectId projectId, DocumentId documentId, object key, TArgs data, int itemCount)
        {
            // reuse factory. it is okay to re-use factory since we make sure we remove the factory before
            // adding it back
            bool newFactory = false;
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            TableEntriesFactory<TData> factory;

            lock (Gate)
            {
                snapshot = Subscriptions;
                GetOrCreateFactory(key, data, out factory, out newFactory);
            }

            factory.OnUpdated(itemCount);

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory);
            }
        }

        private void GetOrCreateFactory(object id, TArgs data, out TableEntriesFactory<TData> factory, out bool newFactory)
        {
            newFactory = false;

            var key = GetKey(id, data);
            if (Map.TryGetValue(key, out factory))
            {
                return;
            }

            var source = CreateTableEntrySource(key, data);
            factory = new TableEntriesFactory<TData>(this, source);

            Map.Add(key, factory);

            newFactory = true;
        }

        protected void ConnectToSolutionCrawlerService(Workspace workspace)
        {
            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            var reporter = crawlerService.GetProgressReporter(workspace);

            // set initial value
            IsStable = !reporter.InProgress;

            ChangeStableState(stable: IsStable);

            reporter.Started += OnSolutionCrawlerStarted;
            reporter.Stopped += OnSolutionCrawlerStopped;
        }

        private void OnSolutionCrawlerStarted(object sender, EventArgs e)
        {
            IsStable = false;
            ChangeStableState(IsStable);
        }

        private void OnSolutionCrawlerStopped(object sender, EventArgs e)
        {
            IsStable = true;
            ChangeStableState(IsStable);
        }
    }
}
