// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal static class FixAllContextHelper
    {
        public static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            FixAllContext fixAllContext,
            IProgressTracker? progressTrackerOpt)
        {
            var cancellationToken = fixAllContext.CancellationToken;

            var allDiagnostics = ImmutableArray<Diagnostic>.Empty;
            var projectsToFix = ImmutableArray<Project>.Empty;

            var document = fixAllContext.Document;
            var project = fixAllContext.Project;

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    if (document != null && !await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
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
                allDiagnostics, projectsToFix, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(
            ImmutableArray<Diagnostic> diagnostics,
            ImmutableArray<Project> projects,
            CancellationToken cancellationToken)
        {
            var treeToDocumentMap = await GetTreeToDocumentMapAsync(projects, cancellationToken).ConfigureAwait(false);

            var builder = ImmutableDictionary.CreateBuilder<Document, ImmutableArray<Diagnostic>>();
            foreach (var (document, diagnosticsForDocument) in diagnostics.GroupBy(d => GetReportedDocument(d, treeToDocumentMap)))
            {
                if (document is null)
                    continue;

                cancellationToken.ThrowIfCancellationRequested();
                if (!await document.IsGeneratedCodeAsync(cancellationToken).ConfigureAwait(false))
                {
                    builder.Add(document, diagnosticsForDocument.ToImmutableArray());
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
                    var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
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

        public static string GetDefaultFixAllTitle(FixAllContext fixAllContext)
            => GetDefaultFixAllTitle(fixAllContext.Scope, fixAllContext.DiagnosticIds, fixAllContext.Document, fixAllContext.Project);

        public static string GetDefaultFixAllTitle(
            FixAllScope fixAllScope,
            ImmutableHashSet<string> diagnosticIds,
            Document? triggerDocument,
            Project triggerProject)
        {
            string diagnosticId;
            if (diagnosticIds.Count == 1)
            {
                diagnosticId = diagnosticIds.Single();
            }
            else
            {
                diagnosticId = string.Join(",", diagnosticIds.ToArray());
            }

            switch (fixAllScope)
            {
                case FixAllScope.Custom:
                    return string.Format(WorkspaceExtensionsResources.Fix_all_0, diagnosticId);

                case FixAllScope.Document:
                    return string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, diagnosticId, triggerDocument!.Name);

                case FixAllScope.Project:
                    return string.Format(WorkspaceExtensionsResources.Fix_all_0_in_1, diagnosticId, triggerProject.Name);

                case FixAllScope.Solution:
                    return string.Format(WorkspaceExtensionsResources.Fix_all_0_in_Solution, diagnosticId);

                default:
                    throw ExceptionUtilities.UnexpectedValue(fixAllScope);
            }
        }
    }
}
