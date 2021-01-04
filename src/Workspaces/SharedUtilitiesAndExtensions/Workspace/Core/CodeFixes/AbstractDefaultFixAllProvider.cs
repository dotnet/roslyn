// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    /// <summary>
    /// Default implementation of a <see cref="FixAllProvider"/> that efficiently handles the dispatch logic for fixing
    /// entire solutions.
    /// </summary>
    internal abstract class AbstractDefaultFixAllProvider : FixAllProvider
    {
        /// <summary>
        /// Gets an appropriate title to show in user facing UI (for example, the title of a progress bar).
        /// </summary>
        protected abstract string GetFixAllTitle(FixAllContext fixAllContext);

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

        private Task<Solution?> GetDocumentFixesAsync(FixAllContext fixAllContext)
            => FixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext));

        private Task<Solution?> GetProjectFixesAsync(FixAllContext fixAllContext)
            => FixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext.WithDocument(null)));

        private Task<Solution?> GetSolutionFixesAsync(FixAllContext fixAllContext)
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
                sortedProjects.SelectAsArray(p => fixAllContext.WithScope(FixAllScope.Project).WithProject(p).WithDocument(null)));
        }

        /// <summary>
        /// All fix-alls funnel into this method.  For doc-fix-all or project-fix-all call into this with just their
        /// single <see cref="FixAllContext"/> in <paramref name="fixAllContexts"/>.  For solution-fix-all, <paramref
        /// name="fixAllContexts"/> will contain a context for each project in the solution.
        /// </summary>
        protected abstract Task<Solution?> FixAllContextsAsync(FixAllContext originalFixAllContext, ImmutableArray<FixAllContext> fixAllContexts);
    }
}
