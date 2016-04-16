// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// A version of ITableDataSource who knows how to connect them to Roslyn solution crawler for live information.
    /// </summary>
    internal abstract class AbstractRoslynTableDataSource<TData> : AbstractTableDataSource<TData>
    {
        public AbstractRoslynTableDataSource(Workspace workspace) : base(workspace)
        {
            ConnectToSolutionCrawlerService(workspace);
        }

        private void ConnectToSolutionCrawlerService(Workspace workspace)
        {
            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            if (crawlerService == null)
            {
                // can happen depends on host such as testing host.
                return;
            }

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
