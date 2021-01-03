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

        public sealed override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
            => base.GetSupportedFixAllScopes();

        public sealed override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project or FixAllScope.Solution);

            var solution = fixAllContext.Scope switch
            {
                FixAllScope.Document => await GetDocumentFixesAsync(fixAllContext).ConfigureAwait(false),
                FixAllScope.Project => await GetProjectFixesAsync(fixAllContext).ConfigureAwait(false),
                FixAllScope.Solution => await GetSolutionFixesAsync(fixAllContext).ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllContext.Scope),
            };

            if (solution == null)
                return null;

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

            return CodeAction.Create(
                GetFixAllTitle(fixAllContext),
                c => Task.FromResult(solution));

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'
        }

        private Task<Solution> GetDocumentFixesAsync(FixAllContext fixAllContext)
            => FixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext));

        private Task<Solution> GetProjectFixesAsync(FixAllContext fixAllContext)
            => FixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext.WithDocument(null)));

        private Task<Solution> GetSolutionFixesAsync(FixAllContext fixAllContext)
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
            //
            // Note: we have to filter down to projects of the same language as the FixAllContext points at a
            // CodeFixProvider, and we can't call into providers of different languages with diagnostics from a
            // different language.
            var sortedProjects = dependencyGraph.GetTopologicallySortedProjects()
                                                .Select(id => solution.GetRequiredProject(id))
                                                .Where(p => p.Language == fixAllContext.Project.Language);
            return FixAllContextsAsync(
                fixAllContext,
                sortedProjects.SelectAsArray(
                    p => fixAllContext.WithScope(FixAllScope.Project).WithProject(p).WithDocument(null)));
        }

        /// <summary>
        /// All fix-alls funnel into this method.  For doc-fix-all or project-fix-all, both call into this with just
        /// their single <see cref="FixAllContext"/> in <paramref name="fixAllContexts"/>.  For solution-fix-all,
        /// <paramref name="fixAllContexts"/> will contain a context for each project in the solution.
        /// </summary>
        private async Task<Solution> FixAllContextsAsync(
            FixAllContext originalFixAllContext,
            ImmutableArray<FixAllContext> fixAllContexts)
        {
            var progressTracker = originalFixAllContext.GetProgressTracker();
            progressTracker.Description = this.GetFixAllTitle(originalFixAllContext);

            var solution = originalFixAllContext.Solution;

            // We have 3 pieces of work per project.  Computing diagnostics, computing fixes, and applying fixes.
            progressTracker.AddItems(fixAllContexts.Length * 3);

            // Process each context one at a time, allowing us to dump any information we computed for each once done with it.
            var currentSolution = solution;
            foreach (var fixAllContext in fixAllContexts)
            {
                Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project);
                currentSolution = await FixSingleContextAsync(currentSolution, fixAllContext).ConfigureAwait(false);
            }

            return currentSolution;
        }

        private async Task<Solution> FixSingleContextAsync(Solution currentSolution, FixAllContext fixAllContext)
        {
            // First, determine the diagnostics to fix.
            var diagnostics = await DetermineDiagnosticsAsync(fixAllContext).ConfigureAwait(false);

            // Second, get the fixes for all the diagnostics.
            var docIdToNewRootOrText = await GetFixedDocumentsAsync(fixAllContext, diagnostics).ConfigureAwait(false);

            // Finally, apply all the fixes to the solution.  This can actually be significant work as we need to
            // cleanup the documents.
            currentSolution = await CleanupAndApplyChangesAsync(fixAllContext, currentSolution, docIdToNewRootOrText).ConfigureAwait(false);

            return currentSolution;
        }

        /// <summary>
        /// Determines all the diagnostics we should be fixing for the given <paramref name="fixAllContext"/>.
        /// </summary>
        private static async Task<ImmutableArray<Diagnostic>> DetermineDiagnosticsAsync(FixAllContext fixAllContext)
        {
            var progressTracker = fixAllContext.GetProgressTracker();
            using var _ = progressTracker.ItemCompletedScope();

            var name = fixAllContext.Document?.Name ?? fixAllContext.Project.Name;
            progressTracker.Description = string.Format(WorkspaceExtensionsResources._0_Computing_diagnostics, name);

            return fixAllContext.Document != null
                ? await fixAllContext.GetDocumentDiagnosticsAsync(fixAllContext.Document).ConfigureAwait(false)
                : await fixAllContext.GetAllDiagnosticsAsync(fixAllContext.Project).ConfigureAwait(false);
        }

        /// <summary>
        /// Attempts to fix all the provided <paramref name="diagnostics"/> returning, for each updated document, either
        /// the new syntax root for that document or its new text.  Syntax roots are returned for documents that support
        /// them, and are used to perform a final cleanup pass for formatting/simplication/etc.  Text is returned for
        /// documents that don't support syntax.
        /// </summary>
        private async Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>> GetFixedDocumentsAsync(
            FixAllContext fixAllContext, ImmutableArray<Diagnostic> diagnostics)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            var progressTracker = fixAllContext.GetProgressTracker();

            using var _1 = progressTracker.ItemCompletedScope();
            using var _2 = ArrayBuilder<Task<(DocumentId, (SyntaxNode? node, SourceText? text))>>.GetInstance(out var tasks);

            var docIdToNewRootOrText = new Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>();
            if (!diagnostics.IsEmpty)
            {
                // Then, once we've got the diagnostics, bucket them by document and the process all documents in
                // parallel to get the change for each doc.
                var name = fixAllContext.Document?.Name ?? fixAllContext.Project.Name;
                progressTracker.Description = string.Format(WorkspaceExtensionsResources._0_Computing_fixes_for_1_diagnostics, name, diagnostics.Length);

                foreach (var group in diagnostics.Where(d => d.Location.IsInSource).GroupBy(d => d.Location.SourceTree))
                {
                    var tree = group.Key;
                    Contract.ThrowIfNull(tree);
                    var document = fixAllContext.Solution.GetRequiredDocument(tree);
                    var documentDiagnostics = group.ToImmutableArray();
                    if (documentDiagnostics.IsDefaultOrEmpty)
                        continue;

                    tasks.Add(Task.Run(async () =>
                    {
                        var newDocument = await this.FixAllAsync(fixAllContext, document, documentDiagnostics).ConfigureAwait(false);
                        if (newDocument == null || newDocument == document)
                            return default;

                        // For documents that support syntax, grab the tree so that we can clean it up later.  If it's a
                        // language that doesn't support that, then just grab the text.
                        var node = newDocument.SupportsSyntaxTree ? await newDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false) : null;
                        var text = newDocument.SupportsSyntaxTree ? null : await newDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);

                        return (document.Id, (node, text));
                    }, cancellationToken));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                foreach (var task in tasks)
                {
                    var (docId, nodeOrText) = await task.ConfigureAwait(false);
                    if (docId != null)
                        docIdToNewRootOrText[docId] = nodeOrText;
                }
            }

            return docIdToNewRootOrText;
        }

        /// <summary>
        /// Take all the fixed documents and format/simplify/clean them up (if the language supports that), and take the
        /// resultant text and apply it to the solution.  If the language doesn't support cleanup, then just take the
        /// given text and apply that instead.
        /// </summary>
        private static async Task<Solution> CleanupAndApplyChangesAsync(
            FixAllContext fixAllContext,
            Solution currentSolution,
            Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)> docIdToNewRootOrText)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            var progressTracker = fixAllContext.GetProgressTracker();

            using var _1 = progressTracker.ItemCompletedScope();
            using var _2 = ArrayBuilder<Task<(DocumentId docId, SourceText sourceText)>>.GetInstance(out var cleanupTasks);

            if (docIdToNewRootOrText.Count > 0)
            {
                // Then, once we've got the diagnostics, compute and apply the fixes for all in parallel to all the
                // affected documents in this project.
                progressTracker.Description = fixAllContext.Document != null
                    ? string.Format(WorkspaceExtensionsResources._0_Applying_fixes, fixAllContext.Document.Name)
                    : string.Format(WorkspaceExtensionsResources._0_Applying_fixes_to_1_documents, fixAllContext.Project.Name, docIdToNewRootOrText.Count);

                // Next, go and insert those all into the solution so all the docs in this particular project point at
                // the new trees (or text).  At this point though, the trees have not been cleaned up.  We don't cleanup
                // the documents as they are created, or one at a time as we add them, as that would cause us to run
                // cleanup on N different solution forks (which would be very expensive).  Instead, by adding all the
                // changed documents to one solution, and hten cleaning *those* we only perform cleanup semantics on one
                // forked solution.
                foreach (var (docId, (newRoot, newText)) in docIdToNewRootOrText)
                {
                    currentSolution = newRoot != null
                        ? currentSolution.WithDocumentSyntaxRoot(docId, newRoot)
                        : currentSolution.WithDocumentText(docId, newText);
                }

                // Next, go and cleanup any trees we inserted. Once we clean the document, we get the text of it and
                // insert that back into the final solution.  This way we can release both the original fixed tree, and
                // the cleaned tree (both of which can be much more expensive than just text).
                //
                // Do this in parallel across all the documents that were fixed.
                foreach (var (docId, (newRoot, _)) in docIdToNewRootOrText)
                {
                    if (newRoot != null)
                    {
                        var dirtyDocument = currentSolution.GetRequiredDocument(docId);
                        cleanupTasks.Add(Task.Run(async () =>
                        {
                            var cleanedDocument = await PostProcessCodeAction.Instance.PostProcessChangesAsync(dirtyDocument, cancellationToken).ConfigureAwait(false);
                            var cleanedText = await cleanedDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            return (dirtyDocument.Id, cleanedText);
                        }, cancellationToken));
                    }
                }

                await Task.WhenAll(cleanupTasks).ConfigureAwait(false);

                // Finally, apply the cleaned documents to the solution.
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
