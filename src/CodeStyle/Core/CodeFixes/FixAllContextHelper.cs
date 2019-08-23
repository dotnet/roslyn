// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class FixAllContextHelper
    {
        public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            FixAllContext fixAllContext,
            IProgressTracker progressTrackerOpt,
            Func<Document, CancellationToken, bool> isGeneratedCode)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            var allDiagnostics = ImmutableArray<Diagnostic>.Empty;
            var projectsToFix = ImmutableArray<Project>.Empty;

            var document = fixAllContext.Document;
            var project = fixAllContext.Project;

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    if (document != null && !isGeneratedCode(document, cancellationToken))
                    {
                        var documentDiagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                        return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty.SetItem(document, documentDiagnostics);
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

                    progressTrackerOpt?.AddItems(projectsToFix.Length);

                    var diagnostics = new ConcurrentDictionary<ProjectId, ImmutableArray<Diagnostic>>();
                    var tasks = new Task[projectsToFix.Length];
                    for (var i = 0; i < projectsToFix.Length; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var projectToFix = projectsToFix[i];
                        tasks[i] = Task.Run(async () =>
                        {
                            var projectDiagnostics = await fixAllContext.GetAllDiagnosticsAsync(projectToFix).ConfigureAwait(false);
                            diagnostics.TryAdd(projectToFix.Id, projectDiagnostics);
                            progressTrackerOpt?.ItemCompleted();
                        }, cancellationToken);
                    }

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    allDiagnostics = allDiagnostics.AddRange(diagnostics.SelectMany(i => i.Value));
                    break;
            }

            if (allDiagnostics.IsEmpty)
            {
                return ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
            }

            return await GetDocumentDiagnosticsToFixAsync(
                allDiagnostics, projectsToFix, isGeneratedCode, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<Project> projects,
            Func<Document, CancellationToken, bool> isGeneratedCode,
            CancellationToken cancellationToken)
        {
            var treeToDocumentMap = await GetTreeToDocumentMapAsync(projects, cancellationToken).ConfigureAwait(false);

            var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
            foreach (var documentAndDiagnostics in diagnostics.GroupBy(d => GetReportedDocument(d, treeToDocumentMap)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var document = documentAndDiagnostics.Key;
                if (!isGeneratedCode(document, cancellationToken))
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
                if (treeToDocumentsMap.TryGetValue(tree, out var document))
                {
                    return document;
                }
            }

            return null;
        }
    }
}
