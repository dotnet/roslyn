// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    /// <summary>
    /// A version of ITableDataSource who knows how to connect them to Roslyn solution crawler for live information.
    /// </summary>
    internal abstract class AbstractRoslynTableDataSource<TData> : AbstractTableDataSource<TData>
    {
        private readonly ProgressReporter _reporterOpt;

        public AbstractRoslynTableDataSource(Workspace workspace, ProgressReporter reporterOpt = null) : base(workspace)
        {
            _reporterOpt = reporterOpt;

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

        protected void ChangeProgress(string message)
        {
            _reporterOpt?.ChangeProgress(message);
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

        private void OnSolutionCrawlerProgressChanged(object sender, bool started)
        {
            SolutionCrawlerProgressChanged(started);
        }

        private void SolutionCrawlerProgressChanged(bool started)
        {
            IsStable = !started;
            ChangeStableState(IsStable);

            _reporterOpt?.Started(started);
        }
    }
}
