// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> that fixes documents independently.
    /// </summary>
    internal abstract class AbstractDocumentBasedFixAllProvider : FixAllProvider
    {
        protected abstract string GetFixAllTitle(FixAllContext fixAllContext);

        protected abstract Task<Document?> FixAllAsync(FixAllContext context, Document document, ImmutableArray<Diagnostic> diagnostics);

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            var title = GetFixAllTitle(fixAllContext);

            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetDocumentFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(DocumentBasedFixAllProvider)));

                case FixAllScope.Project:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetProjectFixesAsync(fixAllContext.WithCancellationToken(c), fixAllContext.Project),
                        nameof(DocumentBasedFixAllProvider)));

                case FixAllScope.Solution:
                    return Task.FromResult<CodeAction?>(CodeAction.Create(
                        title,
                        c => GetSolutionFixesAsync(fixAllContext.WithCancellationToken(c)),
                        nameof(DocumentBasedFixAllProvider)));

                case FixAllScope.Custom:
                default:
                    return Task.FromResult<CodeAction?>(null);
            }
        }

#pragma warning restore RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

        private async Task<Document> GetDocumentFixesAsync(FixAllContext fixAllContext)
        {
            RoslynDebug.AssertNotNull(fixAllContext.Document);

            var document = fixAllContext.Document;
            var documentDiagnosticsToFix = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
            if (!documentDiagnosticsToFix.TryGetValue(document, out var diagnostics))
                return document;

            var newDoc = await this.FixAllAsync(fixAllContext, document, diagnostics).ConfigureAwait(false);
            return newDoc ?? document;
        }

        private Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext, Project project)
            => FixAllInSolutionAsync(fixAllContext, ImmutableArray.Create(project.Id));

        private Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
            => FixAllInSolutionAsync(fixAllContext);

        private Task<Solution> FixAllInSolutionAsync(FixAllContext fixAllContext)
        {
            var solution = fixAllContext.Solution;
            var dependencyGraph = solution.GetProjectDependencyGraph();

            // Walk through each project in topological order, determining and applying the diagnostics for each
            // project.  We do this in topological order so that the compilations for successive projects are readily
            // available as we just computed them for dependent projects.  If we were to do it out of order, we might
            // start with a project that has a ton of dependencies, and we'd spend an inordinate amount of time just
            // building the compilations for it before we could proceed.
            //
            // By processing one project at a time, we can also let go of a project once done with it, allowing us to
            // reclaim lots of the memory so we don't overload the system while processing a large solution.
            var sortedProjectIds = dependencyGraph.GetTopologicallySortedProjects().ToImmutableArray();
            return FixAllInSolutionAsync(fixAllContext, sortedProjectIds);
        }

        private async Task<Solution> FixAllInSolutionAsync(
            FixAllContext fixAllContext,
            ImmutableArray<ProjectId> projectIds)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            var progressTracker = fixAllContext.GetProgressTracker();
            progressTracker.Description = this.GetFixAllTitle(fixAllContext);

            var solution = fixAllContext.Solution;

            // We have 3 pieces of work per project.  Computing diagnostics, computing fixes, and applying fixes.
            progressTracker.AddItems(projectIds.Length * 3);

            var currentSolution = solution;
            foreach (var projectId in projectIds)
            {
                var project = solution.GetRequiredProject(projectId);

                // First, determine the diagnostics to fix.
                var diagnostics = await DetermineDiagnosticsAsync(fixAllContext, project).ConfigureAwait(false);

                // Second, get the fixes for all the diagnostics.
                var docIdToNewRoot = await GetDocumentFixesAsync(fixAllContext, project, diagnostics).ConfigureAwait(false);

                // Finally, apply all the fixes to the solution.  This can actually be significant work as we need to
                // cleanup the documents.
                currentSolution = await ApplyChangesAsync(fixAllContext, currentSolution, project, docIdToNewRoot, cancellationToken).ConfigureAwait(false);
            }

            return currentSolution;
        }

        private static async Task<ImmutableArray<Diagnostic>> DetermineDiagnosticsAsync(FixAllContext fixAllContext, Project project)
        {
            var progressTracker = fixAllContext.GetProgressTracker();
            using var _ = progressTracker.ItemCompletedScope();

            progressTracker.Description = string.Format(WorkspaceExtensionsResources._0_Computing_diagnostics, project.Name);
            return await fixAllContext.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
        }

        private async Task<Dictionary<DocumentId, SyntaxNode>> GetDocumentFixesAsync(
            FixAllContext fixAllContext, Project project, ImmutableArray<Diagnostic> diagnostics)
        {
            var progressTracker = fixAllContext.GetProgressTracker();
            using var _1 = progressTracker.ItemCompletedScope();

            var docIdToNewRoot = new Dictionary<DocumentId, SyntaxNode>();
            if (!diagnostics.IsEmpty)
            {
                // Then, once we've got the diagnostics, compute the fixes for all of them in parallel to all the
                // affected documents in this project.
                progressTracker.Description = string.Format(WorkspaceExtensionsResources._0_Computing_fixes_for_1_diagnostics, project.Name, diagnostics.Length);

                var cancellationToken = fixAllContext.CancellationToken;

                var solution = fixAllContext.Solution;

                using var _2 = ArrayBuilder<Task<(DocumentId, SyntaxNode)>>.GetInstance(out var tasks);
                foreach (var group in diagnostics.Where(d => d.Location.IsInSource).GroupBy(d => d.Location.SourceTree))
                {
                    var tree = group.Key;
                    Contract.ThrowIfNull(tree);
                    var document = solution.GetRequiredDocument(tree);
                    var documentDiagnostics = group.ToImmutableArray();
                    if (documentDiagnostics.IsDefaultOrEmpty)
                        continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        var newDocument = await this.FixAllAsync(fixAllContext, document, documentDiagnostics).ConfigureAwait(false);
                        if (newDocument == null || newDocument == document)
                            return default;

                        return (document.Id, await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                    }));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var task in tasks)
                {
                    var (docId, newRoot) = await task.ConfigureAwait(false);
                    if (docId != null)
                        docIdToNewRoot[docId] = newRoot;
                }
            }

            return docIdToNewRoot;
        }

        private static async Task<Solution> ApplyChangesAsync(
            FixAllContext fixAllContext,
            Solution currentSolution,
            Project project,
            Dictionary<DocumentId, SyntaxNode> docIdToNewRoot,
            CancellationToken cancellationToken)
        {
            var progressTracker = fixAllContext.GetProgressTracker();
            using var _1 = progressTracker.ItemCompletedScope();

            if (docIdToNewRoot.Count > 0)
            {
                // Then, once we've got the diagnostics, compute and apply the fixes for all in parallel to all the
                // affected documents in this project.
                progressTracker.Description = string.Format(WorkspaceExtensionsResources._0_Applying_fixes_to_1_documents, project.Name, docIdToNewRoot.Count);

                // Next, go and insert those all into the solution so all the docs in this particular project point
                // at the new trees.  At this point though, the trees have not been postprocessed/cleaned.
                foreach (var (docId, newRoot) in docIdToNewRoot)
                    currentSolution = currentSolution.WithDocumentSyntaxRoot(docId, newRoot);

                // Next, go and cleanup the trees we inserted.  We do this in bulk so we can benefit from Sharing a
                // single compilation across all of them for all the semantic work we need to do. 
                //
                // Also, once we clean the document, get the text of it and insert that back into the final
                // solution.  This way we can release both the original fixed tree, and the cleaned tree (both of
                // which can be much more expensive than just text).
                using var _2 = ArrayBuilder<Task<(DocumentId docId, SourceText sourceText)>>.GetInstance(out var cleanupTasks);

                foreach (var (docId, _) in docIdToNewRoot)
                {
                    var dirtyDocument = currentSolution.GetRequiredDocument(docId);
                    cleanupTasks.Add(Task.Run(async () =>
                    {
                        var cleanedDocument = await PostProcessCodeAction.Instance.PostProcessChangesAsync(dirtyDocument, cancellationToken).ConfigureAwait(false);
                        var cleanedText = await cleanedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                        return (dirtyDocument.Id, cleanedText);
                    }));
                }

                await Task.WhenAll(cleanupTasks).ConfigureAwait(false);

                foreach (var task in cleanupTasks)
                {
                    var (docId, cleanedText) = await task.ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentText(docId, cleanedText);
                }
            }

            return currentSolution;
        }

        /// <summary>
        /// Dummy class just to get access to <see cref="CodeAction.PostProcessChangesAsync(Document, CancellationToken)"/>
        /// </summary>
        private class PostProcessCodeAction : CodeAction
        {
            public static readonly PostProcessCodeAction Instance = new();

            public override string Title => "";

            public new Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
                => base.PostProcessChangesAsync(document, cancellationToken);
        }
    }
}
