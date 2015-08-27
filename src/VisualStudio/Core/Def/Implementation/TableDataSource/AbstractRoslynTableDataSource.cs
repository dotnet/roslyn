// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class AbstractRoslynTableDataSource<TArgs, TData> : AbstractTableDataSource<TData>
    {
        protected abstract AbstractTableEntriesFactory<TData> CreateTableEntryFactory(object key, TArgs data);

        protected void OnDataAddedOrChanged(object key, Solution solution, ProjectId projectId, DocumentId documentId, TArgs data, int itemCount)
        {
            // reuse factory. it is okay to re-use factory since we make sure we remove the factory before
            // adding it back
            bool newFactory = false;
            ImmutableArray<SubscriptionWithoutLock> snapshot;
            AbstractTableEntriesFactory<TData> factory;

            lock (Gate)
            {
                snapshot = Subscriptions;
                if (!Map.TryGetValue(key, out factory))
                {
                    factory = CreateTableEntryFactory(key, data);
                    Map.Add(key, factory);
                    newFactory = true;
                }
            }

            factory.OnUpdated(itemCount);

            for (var i = 0; i < snapshot.Length; i++)
            {
                snapshot[i].AddOrUpdate(factory, newFactory);
            }
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
