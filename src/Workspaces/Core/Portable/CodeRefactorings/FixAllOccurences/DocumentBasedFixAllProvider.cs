// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.FixAll.CommonDocumentBasedFixAllProviderHelpers;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    using FixAllContexts = Func<FixAllContext, ImmutableArray<FixAllContext>, Task<Solution?>>;

    /// <summary>
    /// Provides a base class to write a <see cref="FixAllProvider"/> for refactorings that fixes documents independently.
    /// This type should be used in the case where the code refactoring(s) only affect individual <see cref="Document"/>s.
    /// </summary>
    /// <remarks>
    /// This type provides suitable logic for fixing large solutions in an efficient manner.  Projects are serially
    /// processed, with all the documents in the project being processed in parallel. 
    /// <see cref="FixAllAsync(FixAllContext)"/> is invoked for each document for implementors to process.
    /// </remarks>
    public abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        protected DocumentBasedFixAllProvider()
        {
        }

        /// <summary>
        /// Produce a suitable title for the fix-all <see cref="CodeAction"/> this type creates in <see
        /// cref="GetFixAsync(FixAllContext)"/>.  Override this if customizing that title is desired.
        /// </summary>
        protected virtual string GetFixAllTitle(FixAllContext fixAllContext)
            => FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);

        /// <summary>
        /// Apply fix all operation for the <see cref="FixAllContext.CodeAction"/> in the <see cref="FixAllContext.Document"/>
        /// for the given <paramref name="fixAllContext"/>.  The document returned will only be examined for its content
        /// (e.g. it's <see cref="SyntaxTree"/> or <see cref="SourceText"/>.  No other aspects of document (like it's properties),
        /// or changes to the <see cref="Project"/> or <see cref="Solution"/> it points at will be considered.
        /// </summary>
        /// <param name="fixAllContext">The context for the Fix All operation.</param>
        /// <returns>
        /// <para>The new <see cref="Document"/> representing the content fixed document.</para>
        /// <para>-or-</para>
        /// <para><see langword="null"/>, if no changes were made to the document.</para>
        /// </returns>
        protected abstract Task<Document?> FixAllAsync(FixAllContext fixAllContext);

        /// <summary>
        /// Returns a bool indicating if the provider supports FixAll in selected span,
        /// i.e. <see cref="FixAllScope.Selection"/>
        /// </summary>
        protected abstract bool SupportsFixAllForSelection { get; }

        /// <summary>
        /// Returns a bool indicating if the provider supports FixAll in containing member,
        /// i.e. <see cref="FixAllScope.ContainingMember"/>
        /// </summary>
        protected abstract bool SupportsFixAllForContainingMember { get; }

        /// <summary>
        /// Returns a bool indicating if the provider supports FixAll in containing type declaration,
        /// i.e. <see cref="FixAllScope.ContainingType"/>
        /// </summary>
        protected abstract bool SupportsFixAllForContainingType { get; }

        public sealed override IEnumerable<FixAllScope> GetSupportedFixAllScopes()
        {
            foreach (var defaultScope in base.GetSupportedFixAllScopes())
                yield return defaultScope;

            if (SupportsFixAllForSelection)
                yield return FixAllScope.Selection;

            if (SupportsFixAllForContainingMember)
                yield return FixAllScope.ContainingMember;

            if (SupportsFixAllForContainingType)
                yield return FixAllScope.ContainingType;
        }

        public sealed override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => GetFixAsync(FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext), fixAllContext, FixAllContextsAsync);

        private static async Task<CodeAction?> GetFixAsync(
            string title, FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
        {
            Contract.ThrowIfFalse(fixAllContext.Scope is
                FixAllScope.Document or FixAllScope.Project or FixAllScope.Solution or
                FixAllScope.Selection or FixAllScope.ContainingMember or FixAllScope.ContainingType);

            var solution = fixAllContext.Scope switch
            {
                FixAllScope.Document or FixAllScope.Selection or
                FixAllScope.ContainingMember or FixAllScope.ContainingType
                    => await GetDocumentFixesAsync(fixAllContext, fixAllContextsAsync).ConfigureAwait(false),
                FixAllScope.Project
                    => await GetProjectFixesAsync(fixAllContext, fixAllContextsAsync).ConfigureAwait(false),
                FixAllScope.Solution
                    => await GetSolutionFixesAsync(fixAllContext, fixAllContextsAsync).ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllContext.Scope),
            };

            if (solution == null)
                return null;

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

            return CodeAction.Create(
                title, c => Task.FromResult(solution));

#pragma warning restore RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'
        }

        private static Task<Solution?> GetDocumentFixesAsync(FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
            => fixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext));

        private static Task<Solution?> GetProjectFixesAsync(FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
            => fixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext.WithDocument(null)));

        private static Task<Solution?> GetSolutionFixesAsync(FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
        {
            var solution = fixAllContext.Project.Solution;
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
            return fixAllContextsAsync(
                fixAllContext,
                sortedProjects.SelectAsArray(p => fixAllContext.WithScope(FixAllScope.Project).WithProject(p).WithDocument(null)));
        }

        private async Task<Solution?> FixAllContextsAsync(FixAllContext originalFixAllContext, ImmutableArray<FixAllContext> fixAllContexts)
        {
            var progressTracker = originalFixAllContext.GetProgressTracker();
            progressTracker.Description = this.GetFixAllTitle(originalFixAllContext);

            var solution = originalFixAllContext.Project.Solution;

            // We have 2 pieces of work per project.  Computing code actions, and applying actions.
            progressTracker.AddItems(fixAllContexts.Length * 2);

            // Process each context one at a time, allowing us to dump any information we computed for each once done with it.
            var currentSolution = solution;
            foreach (var fixAllContext in fixAllContexts)
            {
                Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project or
                    FixAllScope.Selection or FixAllScope.ContainingMember or FixAllScope.ContainingType);
                currentSolution = await FixSingleContextAsync(currentSolution, fixAllContext, progressTracker).ConfigureAwait(false);
            }

            return currentSolution;
        }

        private async Task<Solution> FixSingleContextAsync(Solution currentSolution, FixAllContext fixAllContext, IProgressTracker progressTracker)
        {
            // First, get the fixes for all the diagnostics, and apply them to determine the new root/text for each doc.
            var docIdToNewRootOrText = await GetFixedDocumentsAsync(fixAllContext, progressTracker).ConfigureAwait(false);

            // Finally, cleanup the new doc roots, and apply the results to the solution.
            currentSolution = await CleanupAndApplyChangesAsync(progressTracker, currentSolution, docIdToNewRootOrText, fixAllContext.CancellationToken).ConfigureAwait(false);

            return currentSolution;
        }

        /// <summary>
        /// Attempts to apply fix all operations returning, for each updated document, either
        /// the new syntax root for that document or its new text.  Syntax roots are returned for documents that support
        /// them, and are used to perform a final cleanup pass for formatting/simplication/etc.  Text is returned for
        /// documents that don't support syntax.
        /// </summary>
        private async Task<Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>> GetFixedDocumentsAsync(
            FixAllContext fixAllContext, IProgressTracker progressTracker)
        {
            Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project
                or FixAllScope.Selection or FixAllScope.ContainingMember or FixAllScope.ContainingType);

            var cancellationToken = fixAllContext.CancellationToken;

            using var _1 = progressTracker.ItemCompletedScope();
            using var _2 = ArrayBuilder<Task<(DocumentId, (SyntaxNode? node, SourceText? text))>>.GetInstance(out var tasks);

            var docIdToNewRootOrText = new Dictionary<DocumentId, (SyntaxNode? node, SourceText? text)>();

            // Process all documents in parallel to get the change for each doc.
            var documentsToFix = fixAllContext.Scope == FixAllScope.Project
                ? fixAllContext.Project.Documents
                : SpecializedCollections.SingletonEnumerable(fixAllContext.Document);

            foreach (var document in documentsToFix)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var newFixAllContext = fixAllContext.WithDocument(document);
                    var newDocument = await this.FixAllAsync(fixAllContext).ConfigureAwait(false);
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

            return docIdToNewRootOrText;
        }
    }
}
