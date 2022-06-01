// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// A version of ITableDataSource who knows how to connect them to Roslyn solution crawler for live information.
    /// </summary>
    internal abstract class AbstractRoslynTableDataSource<TItem, TData> : AbstractTableDataSource<TItem, TData>
        where TItem : TableItem
        where TData : notnull
    {
        public AbstractRoslynTableDataSource(Workspace workspace, IThreadingContext threadingContext)
            : base(workspace, threadingContext)
            => ConnectToSolutionCrawlerService(workspace);

        protected ImmutableArray<DocumentId> GetDocumentsWithSameFilePath(Solution solution, DocumentId documentId)
        {
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return ImmutableArray<DocumentId>.Empty;
            }

            return solution.GetDocumentIdsWithFilePath(document.FilePath);
        }

        /// <summary>
        /// Flag indicating if a solution crawler is running incremental analyzers in background.
        /// We get build progress updates from <see cref="ISolutionCrawlerProgressReporter.ProgressChanged"/>.
        /// Solution crawler progress events are guaranteed to be invoked in a serial fashion.
        /// </summary>
        protected bool IsSolutionCrawlerRunning { get; private set; }

        private void ConnectToSolutionCrawlerService(Workspace workspace)
        {
            var crawlerService = workspace.Services.GetService<ISolutionCrawlerService>();
            if (crawlerService == null)
            {
                // can happen depends on host such as testing host.
                return;
            }

            var reporter = crawlerService.GetProgressReporter(workspace);
            reporter.ProgressChanged += OnSolutionCrawlerProgressChanged;

            // set initial value
            SolutionCrawlerProgressChanged(reporter.InProgress);
        }

        private void OnSolutionCrawlerProgressChanged(object sender, ProgressData progressData)
        {
            switch (progressData.Status)
            {
                case ProgressStatus.Started:
                    SolutionCrawlerProgressChanged(running: true);
                    break;
                case ProgressStatus.Stopped:
                    SolutionCrawlerProgressChanged(running: false);
                    break;
            }
        }

        private void SolutionCrawlerProgressChanged(bool running)
        {
            IsSolutionCrawlerRunning = running;
            ChangeStableStateIfRequired(newIsStable: !IsSolutionCrawlerRunning);
        }

        protected void ChangeStableStateIfRequired(bool newIsStable)
        {
            var oldIsStable = IsStable;
            if (oldIsStable != newIsStable)
            {
                IsStable = newIsStable;
                ChangeStableState(newIsStable);
            }
        }
    }
}
