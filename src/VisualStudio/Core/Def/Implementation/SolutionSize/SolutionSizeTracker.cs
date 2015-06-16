// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionSize
{
    /// <summary>
    /// Track approximate solution size.
    /// </summary>
    [Export]
    [ExportIncrementalAnalyzerProvider(WorkspaceKind.Host), Shared]
    internal class SolutionSizeTracker : IIncrementalAnalyzerProvider
    {
        private readonly IncrementalAnalyzer _tracker = new IncrementalAnalyzer();

        public long GetSolutionSize(Workspace workspace, SolutionId solutionId)
        {
            return workspace is VisualStudioWorkspaceImpl ? _tracker.GetSolutionSize(solutionId) : -1;
        }

        IIncrementalAnalyzer IIncrementalAnalyzerProvider.CreateIncrementalAnalyzer(Workspace workspace)
        {
            return workspace is VisualStudioWorkspaceImpl ? _tracker : null;
        }

        internal class IncrementalAnalyzer : IIncrementalAnalyzer
        {
            private readonly ConcurrentDictionary<DocumentId, long> _map = new ConcurrentDictionary<DocumentId, long>(concurrencyLevel: 2, capacity: 10);

            private SolutionId _solutionId;
            private long _size;

            public long GetSolutionSize(SolutionId solutionId)
            {
                return _solutionId == solutionId ? _size : -1;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                if (_solutionId != solution.Id)
                {
                    _size = 0;
                    _map.Clear();

                    _solutionId = solution.Id;
                }

                return SpecializedTasks.EmptyTask;
            }

            public async Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
            {
                // getting tree is cheap since tree always stays in memory
                var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

                // remove old size
                long size;
                if (_map.TryGetValue(document.Id, out size))
                {
                    _size -= size;
                }

                // add new size
                _map[document.Id] = tree.Length;
                _size += tree.Length;
            }

            public void RemoveDocument(DocumentId documentId)
            {
                long size;
                if (_map.TryRemove(documentId, out size))
                {
                    _size -= size;
                }
            }

            #region Not Used
            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public void RemoveProject(ProjectId projectId)
            {
            }
            #endregion
        }
    }
}
