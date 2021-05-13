// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable disable warnings

namespace Analyzer.Utilities
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;

    internal static class FixAllContextHelper
    {
        public static async Task<ImmutableDictionary<Document?, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(FixAllContext fixAllContext)
        {
            var allDiagnostics = ImmutableArray<Diagnostic>.Empty;
            var projectsToFix = ImmutableArray<Project>.Empty;

            var document = fixAllContext.Document;
            var project = fixAllContext.Project;

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    if (document != null)
                    {
                        var documentDiagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                        return ImmutableDictionary<Document?, ImmutableArray<Diagnostic>>.Empty.SetItem(document, documentDiagnostics);
                    }

                    break;

                case FixAllScope.Project:
                    projectsToFix = ImmutableArray.Create(project);
                    allDiagnostics = await GetAllDiagnosticsAsync(fixAllContext, project).ConfigureAwait(false);
                    break;

                case FixAllScope.Solution:
                    projectsToFix = project.Solution.Projects
                        .Where(p => p.Language == project.Language)
                        .ToImmutableArray();

                    var diagnostics = new ConcurrentDictionary<ProjectId, ImmutableArray<Diagnostic>>();
                    var tasks = new Task[projectsToFix.Length];
                    for (int i = 0; i < projectsToFix.Length; i++)
                    {
                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                        var projectToFix = projectsToFix[i];
                        tasks[i] = Task.Run(
                            async () =>
                            {
                                var projectDiagnostics = await GetAllDiagnosticsAsync(fixAllContext, projectToFix).ConfigureAwait(false);
                                diagnostics.TryAdd(projectToFix.Id, projectDiagnostics);
                            }, fixAllContext.CancellationToken);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    allDiagnostics = allDiagnostics.AddRange(diagnostics.SelectMany(i => i.Value.Where(x => fixAllContext.DiagnosticIds.Contains(x.Id))));
                    break;
            }

            if (allDiagnostics.IsEmpty)
            {
                return ImmutableDictionary<Document?, ImmutableArray<Diagnostic>>.Empty;
            }

            return await GetDocumentDiagnosticsToFixAsync(allDiagnostics, projectsToFix, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        public static async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(FixAllContext fixAllContext)
        {
            var project = fixAllContext.Project;
            if (project != null)
            {
                switch (fixAllContext.Scope)
                {
                    case FixAllScope.Project:
                        var diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
                        return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty.SetItem(project, diagnostics);

                    case FixAllScope.Solution:
                        var projectsAndDiagnostics = new ConcurrentDictionary<Project, ImmutableArray<Diagnostic>>();
                        var tasks = new List<Task>(project.Solution.ProjectIds.Count);
                        Func<Project, Task> projectAction =
                            async proj =>
                            {
                                if (fixAllContext.CancellationToken.IsCancellationRequested)
                                {
                                    return;
                                }

                                var projectDiagnostics = await fixAllContext.GetProjectDiagnosticsAsync(proj).ConfigureAwait(false);
                                if (projectDiagnostics.Any())
                                {
                                    projectsAndDiagnostics.TryAdd(proj, projectDiagnostics);
                                }
                            };

                        foreach (var proj in project.Solution.Projects)
                        {
                            if (fixAllContext.CancellationToken.IsCancellationRequested)
                            {
                                break;
                            }

                            tasks.Add(projectAction(proj));
                        }

                        await Task.WhenAll(tasks).ConfigureAwait(false);
                        return projectsAndDiagnostics.ToImmutableDictionary();
                }
            }

            return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
        }

        /// <summary>
        /// Gets all <see cref="Diagnostic"/> instances within a specific <see cref="Project"/> which are relevant to a
        /// <see cref="FixAllContext"/>.
        /// </summary>
        /// <param name="fixAllContext">The context for the Fix All operation.</param>
        /// <param name="project">The project.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the asynchronous operation. When the task completes
        /// successfully, the <see cref="Task{TResult}.Result"/> will contain the requested diagnostics.</returns>
        private static async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(FixAllContext fixAllContext, Project project)
        {
            return await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
        }

        private static async Task<ImmutableDictionary<Document?, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<Project> projects,
            CancellationToken cancellationToken)
        {
            var treeToDocumentMap = await GetTreeToDocumentMapAsync(projects, cancellationToken).ConfigureAwait(false);

            var builder = ImmutableDictionary.CreateBuilder<Document?, ImmutableArray<Diagnostic>>();
            foreach (var documentAndDiagnostics in diagnostics.GroupBy(d => GetReportedDocument(d, treeToDocumentMap)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = documentAndDiagnostics.Key;
                var diagnosticsForDocument = documentAndDiagnostics.ToImmutableArray();
                builder.Add(document, diagnosticsForDocument);
            }

            return builder.ToImmutable();
        }

        private static async Task<ImmutableDictionary<SyntaxTree, Document>> GetTreeToDocumentMapAsync(ImmutableArray<Project> projects, CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<SyntaxTree, Document>();
            foreach (var project in projects)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var document in project.Documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    builder.Add(tree, document);
                }
            }

            return builder.ToImmutable();
        }

        private static Document? GetReportedDocument(Diagnostic diagnostic, ImmutableDictionary<SyntaxTree, Document> treeToDocumentsMap)
        {
            var tree = diagnostic.Location.SourceTree;
            if (tree != null)
            {
                if (treeToDocumentsMap.TryGetValue(tree, out var document))
                {
                    return document;
                }
            }

            return null;
        }
    }
}
