// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private async Task ProcessProjectsAsync(
            IEnumerable<ProjectId> projectSet,
            Dictionary<Project, Dictionary<Document, List<ValueTuple<SymbolAndProjectId, IReferenceFinder>>>> projectMap,
            ProgressWrapper wrapper)
        {
            var visitedProjects = new HashSet<ProjectId>();

            // Make sure we process each project in the set.  Process each project in depth first
            // order.  That way when we process a project, the compilations for all projects that it
            // depends on will have been created already.
            foreach (var projectId in projectSet)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await ProcessProjectAsync(projectId, projectMap, visitedProjects, wrapper).ConfigureAwait(false);
                }
                finally
                {
                    await _progressTracker.ItemCompletedAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task ProcessProjectAsync(
            ProjectId projectId,
            Dictionary<Project, Dictionary<Document, List<ValueTuple<SymbolAndProjectId, IReferenceFinder>>>> projectMap,
            HashSet<ProjectId> visitedProjects,
            ProgressWrapper wrapper)
        {
            // Don't visit projects more than once.  
            if (visitedProjects.Add(projectId))
            {
                var project = _solution.GetProject(projectId);

                // Visit dependencies first.  That way the compilation for a project that we depend
                // on is already ready for us when we need it.
                foreach (var dependent in project.ProjectReferences)
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    await ProcessProjectAsync(dependent.ProjectId, projectMap, visitedProjects, wrapper).ConfigureAwait(false);
                }

                await ProcessProjectAsync(project, projectMap, wrapper).ConfigureAwait(false);
            }
        }

        private async Task ProcessProjectAsync(
            Project project,
            Dictionary<Project, Dictionary<Document, List<ValueTuple<SymbolAndProjectId, IReferenceFinder>>>> projectMap,
            ProgressWrapper wrapper)
        {
            Dictionary<Document, List<ValueTuple<SymbolAndProjectId, IReferenceFinder>>> map;
            if (!projectMap.TryGetValue(project, out map))
            {
                // No files in this project to process.  We can bail here.  We'll have cached our
                // compilation if there are any projects left to process that depend on us.
                return;
            }

            // Now actually process the project.
            await ProcessProjectAsync(project, map, wrapper).ConfigureAwait(false);

            // We've now finished working on the project.  Remove it from the set of remaining items.
            projectMap.Remove(project);
        }

        private async Task ProcessProjectAsync(
            Project project,
            Dictionary<Document, List<ValueTuple<SymbolAndProjectId, IReferenceFinder>>> map,
            ProgressWrapper wrapper)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessProjectAsync, project.Name, _cancellationToken))
            {
                // make sure we hold onto compilation while we search documents belong to this project
                var compilation = await project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);

                var documentTasks = new List<Task>();
                foreach (var kvp in map)
                {
                    var document = kvp.Key;
                    var documentQueue = kvp.Value;

                    documentTasks.Add(Task.Run(() => ProcessDocumentQueueAsync(document, documentQueue, wrapper), _cancellationToken));
                }

                await Task.WhenAll(documentTasks).ConfigureAwait(false);

                GC.KeepAlive(compilation);
            }
        }
    }
}
