// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    /// <summary>
    /// A version of ITableDataSource who knows how to connect them to Roslyn solution crawler for live information.
    /// </summary>
    internal abstract class AbstractRoslynTableDataSource<TItem> : AbstractTableDataSource<TItem>
        where TItem : TableItem
    {
        public AbstractRoslynTableDataSource(Workspace workspace) : base(workspace)
        {
            ConnectToSolutionCrawlerService(workspace);
        }

        protected ImmutableArray<DocumentId> GetDocumentsWithSameFilePath(Solution solution, DocumentId documentId)
        {
            var document = solution.GetDocument(documentId);
            if (document == null)
            {
                return ImmutableArray<DocumentId>.Empty;
            }

            return solution.GetDocumentIdsWithFilePath(document.FilePath);
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
                case ProgressStatus.Stoped:
                    SolutionCrawlerProgressChanged(running: false);
                    break;
            }
        }

        private void SolutionCrawlerProgressChanged(bool running)
        {
            IsStable = !running;
            ChangeStableState(IsStable);
        }
    }
}
