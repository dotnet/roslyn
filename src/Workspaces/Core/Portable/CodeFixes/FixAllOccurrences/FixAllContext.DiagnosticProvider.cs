// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Context for "Fix all occurrences" code fixes provided by an <see cref="FixAllProvider"/>.
    /// </summary>
    public partial class FixAllContext
    {
        /// <summary>
        /// Diagnostic provider to fetch document/project diagnostics to fix in a <see cref="FixAllContext"/>.
        /// </summary>
        public abstract class DiagnosticProvider
        {
            internal virtual bool IsFixMultiple => false;

            /// <summary>
            /// Gets all the diagnostics to fix in the given document in a <see cref="FixAllContext"/>.
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken);

            /// <summary>
            /// Gets all the project-level diagnostics to fix, i.e. diagnostics with no source location, in the given project in a <see cref="FixAllContext"/>.
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken);

            /// <summary>
            /// Gets all the diagnostics to fix in the given project in a <see cref="FixAllContext"/>.
            /// This includes both document-level diagnostics for all documents in the given project and project-level diagnostics, i.e. diagnostics with no source location, in the given project. 
            /// </summary>
            public abstract Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken);

            internal virtual async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
                FixAllContext fixAllContext)
            {
                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, fixAllContext.CancellationToken))
                {
                    var allDiagnostics = ImmutableArray<Diagnostic>.Empty;
                    var projectsToFix = ImmutableArray<Project>.Empty;

                    var document = fixAllContext.Document;
                    var project = fixAllContext.Project;
                    var generatedCodeServices = project.Solution.Workspace.Services.GetService<IGeneratedCodeRecognitionService>();

                    switch (fixAllContext.Scope)
                    {
                        case FixAllScope.Document:
                            if (document != null && !generatedCodeServices.IsGeneratedCode(document))
                            {
                                var documentDiagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                                var kvp = SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(document, documentDiagnostics));
                                return ImmutableDictionary.CreateRange(kvp);
                            }

                            break;

                        case FixAllScope.Project:
                            projectsToFix = ImmutableArray.Create(project);
                            allDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                            break;

                        case FixAllScope.Solution:
                            projectsToFix = project.Solution.Projects
                                .Where(p => p.Language == project.Language)
                                .ToImmutableArray();

                            var progressTracker = fixAllContext.ProgressTracker;
                            progressTracker.AddItems(projectsToFix.Length);

                            var diagnostics = new ConcurrentBag<Diagnostic>();
                            var tasks = new Task[projectsToFix.Length];
                            for (int i = 0; i < projectsToFix.Length; i++)
                            {
                                fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                                var projectToFix = projectsToFix[i];
                                tasks[i] = Task.Run(async () =>
                                {
                                    var projectDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(projectToFix).ConfigureAwait(false);
                                    foreach (var diagnostic in projectDiagnostics)
                                    {
                                        fixAllContext.CancellationToken.ThrowIfCancellationRequested();
                                        diagnostics.Add(diagnostic);
                                    }

                                    progressTracker.ItemCompleted();
                                }, fixAllContext.CancellationToken);
                            }

                            await Task.WhenAll(tasks).ConfigureAwait(false);
                            allDiagnostics = allDiagnostics.AddRange(diagnostics);
                            break;
                    }

                    if (allDiagnostics.IsEmpty)
                    {
                        return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
                    }

                    return await GetDocumentDiagnosticsToFixAsync(allDiagnostics, projectsToFix, generatedCodeServices.IsGeneratedCode, fixAllContext.CancellationToken).ConfigureAwait(false);
                }
            }

            private async static Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
                ImmutableArray<Diagnostic> diagnostics,
                ImmutableArray<Project> projects,
                Func<Document, bool> isGeneratedCode, CancellationToken cancellationToken)
            {
                var treeToDocumentMap = await GetTreeToDocumentMapAsync(projects, cancellationToken).ConfigureAwait(false);

                var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
                foreach (var documentAndDiagnostics in diagnostics.GroupBy(d => GetReportedDocument(d, treeToDocumentMap)))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var document = documentAndDiagnostics.Key;
                    if (!isGeneratedCode(document))
                    {
                        var diagnosticsForDocument = documentAndDiagnostics.ToImmutableArray();
                        builder.Add(document, diagnosticsForDocument);
                    }
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

            private static Document GetReportedDocument(Diagnostic diagnostic, ImmutableDictionary<SyntaxTree, Document> treeToDocumentsMap)
            {
                var tree = diagnostic.Location.SourceTree;
                if (tree != null)
                {
                    Document document;
                    if (treeToDocumentsMap.TryGetValue(tree, out document))
                    {
                        return document;
                    }
                }

                return null;
            }

            internal virtual async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetProjectDiagnosticsToFixAsync(
                FixAllContext fixAllContext)
            {
                using (Logger.LogBlock(FunctionId.CodeFixes_FixAllOccurrencesComputation_Diagnostics, fixAllContext.CancellationToken))
                {
                    var project = fixAllContext.Project;
                    if (project != null)
                    {
                        switch (fixAllContext.Scope)
                        {
                            case FixAllScope.Project:
                                var diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
                                var kvp = SpecializedCollections.SingletonEnumerable(KeyValuePair.Create(project, diagnostics));
                                return ImmutableDictionary.CreateRange(kvp);

                            case FixAllScope.Solution:
                                var projectsAndDiagnostics = ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();

                                var tasks = project.Solution.Projects.Select(async p => new
                                {
                                    Project = p,
                                    Diagnostics = await fixAllContext.GetProjectDiagnosticsAsync(p).ConfigureAwait(false)
                                }).ToArray();

                                await Task.WhenAll(tasks).ConfigureAwait(false);

                                foreach (var task in tasks)
                                {
                                    if (task.Result.Diagnostics.Any())
                                    {
                                        projectsAndDiagnostics[task.Result.Project] = task.Result.Diagnostics;
                                    }
                                }

                                return projectsAndDiagnostics.ToImmutable();
                        }
                    }

                    return ImmutableDictionary<Project, ImmutableArray<Diagnostic>>.Empty;
                }
            }
        }
    }
}