// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    using DocumentMap = MultiDictionary<Document, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>;
    using ProjectToDocumentMap = Dictionary<Project, MultiDictionary<Document, (SymbolAndProjectId symbolAndProjectId, IReferenceFinder finder)>>;

    internal partial class FindReferencesSearchEngine
    {
        private async Task ProcessProjectsAsync(
            IEnumerable<ProjectId> connectedProjectSet,
            ProjectToDocumentMap projectToDocumentMap)
        {
            var visitedProjects = new HashSet<ProjectId>();

            // Make sure we process each project in the set.  Process each project in depth first
            // order.  That way when we process a project, the compilations for all projects that it
            // depends on will have been created already.
            foreach (var projectId in connectedProjectSet)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                await ProcessProjectAsync(
                    projectId, projectToDocumentMap, visitedProjects).ConfigureAwait(false);
            }
        }

        private async Task ProcessProjectAsync(
            ProjectId projectId,
            ProjectToDocumentMap projectToDocumentMap,
            HashSet<ProjectId> visitedProjects)
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

                    await ProcessProjectAsync(
                        dependent.ProjectId, projectToDocumentMap, visitedProjects).ConfigureAwait(false);
                }

                await ProcessProjectAsync(project, projectToDocumentMap).ConfigureAwait(false);
            }
        }

        private async Task ProcessProjectAsync(
            Project project,
            ProjectToDocumentMap projectToDocumentMap)
        {
            if (!projectToDocumentMap.TryGetValue(project, out var documentMap))
            {
                // No files in this project to process.  We can bail here.  We'll have cached our
                // compilation if there are any projects left to process that depend on us.
                return;
            }

            projectToDocumentMap.Remove(project);

            // Now actually process the project.
            await ProcessProjectAsync(project, documentMap).ConfigureAwait(false);
        }

        private async Task ProcessProjectAsync(
            Project project,
            DocumentMap documentMap)
        {
            using (Logger.LogBlock(FunctionId.FindReference_ProcessProjectAsync, project.Name, _cancellationToken))
            {
                if (project.SupportsCompilation)
                {
                    // make sure we hold onto compilation while we search documents belong to this project
                    var compilation = await project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);

                    var documentTasks = new List<Task>();
                    foreach (var kvp in documentMap)
                    {
                        var document = kvp.Key;

                        if (document.Project == project)
                        {
                            var documentQueue = kvp.Value;

                            documentTasks.Add(Task.Run(() => ProcessDocumentQueueAsync(
                                document, documentQueue), _cancellationToken));
                        }
                    }

                    await Task.WhenAll(documentTasks).ConfigureAwait(false);

                    GC.KeepAlive(compilation);
                }
            }
        }
    }
}
