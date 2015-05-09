// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Composition;
using System.IO;
using System.Linq;
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

        private class IncrementalAnalyzer : IIncrementalAnalyzer
        {
            private readonly ConcurrentDictionary<ProjectId, long> _map = new ConcurrentDictionary<ProjectId, long>(concurrencyLevel: 2, capacity: 10);

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
                    _map.Clear();

                    _solutionId = solution.Id;
                }

                return SpecializedTasks.EmptyTask;
            }

            public async Task AnalyzeProjectAsync(Project project, bool semanticsChanged, CancellationToken cancellationToken)
            {
                var sum = await GetProjectSizeAsync(project, cancellationToken).ConfigureAwait(false);

                _map.AddOrUpdate(project.Id, sum, (id, existing) => sum);

                _size = _map.Values.Sum();
            }

            public void RemoveProject(ProjectId projectId)
            {
                long unused;
                _map.TryRemove(projectId, out unused);

                _size = _map.Values.Sum();
            }

            private static async Task<long> GetProjectSizeAsync(Project project, CancellationToken cancellationToken)
            {
                if (project == null)
                {
                    return 0;
                }

                var sum = 0L;
                foreach (var document in project.Documents)
                {
                    sum += await GetDocumentSizeAsync(document, cancellationToken).ConfigureAwait(false);
                }

                return sum;
            }

            private static async Task<long> GetDocumentSizeAsync(Document document, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (document == null)
                {
                    return 0;
                }

                var result = GetFileSize(document.FilePath);
                if (result >= 0)
                {
                    return result;
                }

                // not a physical file, in that case, use text as a fallback.
                var text = await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
                return text.Length;
            }

            private static long GetFileSize(string filepath)
            {
                if (filepath == null)
                {
                    return -1;
                }

                try
                {
                    // just to reduce exception thrown
                    if (!File.Exists(filepath))
                    {
                        return -1;
                    }

                    return new FileInfo(filepath).Length;
                }
                catch
                {
                    return -1;
                }
            }

            #region Not Used
            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, CancellationToken cancellationToken)
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

            public void RemoveDocument(DocumentId documentId)
            {
            }
            #endregion
        }
    }
}
