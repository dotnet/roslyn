// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class DiagnosticAnalyzerService
    {
        /// <summary>
        /// Start new Batch build diagnostics update token.
        /// </summary>
        public IDisposable BeginBatchBuildDiagnosticsUpdate(Solution solution)
        {
            return new BatchUpdateToken(solution);
        }

        /// <summary>
        /// Synchronize build errors with live error.
        /// 
        /// no cancellationToken since this can't be cancelled
        /// </summary>
        public Task SynchronizeWithBuildAsync(IDisposable batchUpdateToken, Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            var token = (BatchUpdateToken)batchUpdateToken;
            token.CheckProjectInSnapshot(project);

            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(project.Solution.Workspace, out analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(token, project, diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }

        /// <summary>
        /// Synchronize build errors with live error
        /// 
        /// no cancellationToken since this can't be cancelled
        /// </summary>
        public Task SynchronizeWithBuildAsync(IDisposable batchUpdateToken, Document document, ImmutableArray<DiagnosticData> diagnostics)
        {
            var token = (BatchUpdateToken)batchUpdateToken;
            token.CheckDocumentInSnapshot(document);

            BaseDiagnosticIncrementalAnalyzer analyzer;
            if (_map.TryGetValue(document.Project.Solution.Workspace, out analyzer))
            {
                return analyzer.SynchronizeWithBuildAsync(token, document, diagnostics);
            }

            return SpecializedTasks.EmptyTask;
        }

        public class BatchUpdateToken : IDisposable
        {
            public readonly ConcurrentDictionary<object, object> _cache = new ConcurrentDictionary<object, object>(concurrencyLevel: 2, capacity: 1);
            private readonly Solution _solution;

            public BatchUpdateToken(Solution solution)
            {
                _solution = solution;
            }

            public object GetCache(object key, Func<object, object> cacheCreator)
            {
                return _cache.GetOrAdd(key, cacheCreator);
            }

            public void CheckDocumentInSnapshot(Document document)
            {
                Contract.ThrowIfFalse(_solution.GetDocument(document.Id) == document);
            }

            public void CheckProjectInSnapshot(Project project)
            {
                Contract.ThrowIfFalse(_solution.GetProject(project.Id) == project);
            }

            public void Dispose()
            {
                _cache.Clear();
            }
        }
    }
}
