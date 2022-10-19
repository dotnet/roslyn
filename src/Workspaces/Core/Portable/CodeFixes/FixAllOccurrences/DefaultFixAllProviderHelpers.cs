// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    using FixAllContexts = Func<FixAllContext, ImmutableArray<FixAllContext>, Task<Solution?>>;

    /// <summary>
    /// Default implementation of a <see cref="FixAllProvider"/> that efficiently handles the dispatch logic for fixing
    /// entire solutions.  Used by <see cref="BatchFixAllProvider"/> and <see cref="DocumentBasedFixAllProvider"/>.
    /// </summary>
    internal static class DefaultFixAllProviderHelpers
    {
        public static async Task<CodeAction?> GetFixAsync(
            string title, FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
        {
            Contract.ThrowIfFalse(fixAllContext.Scope is FixAllScope.Document or FixAllScope.Project or FixAllScope.Solution);

            var solution = fixAllContext.Scope switch
            {
                FixAllScope.Document => await GetDocumentFixesAsync(fixAllContext, fixAllContextsAsync).ConfigureAwait(false),
                FixAllScope.Project => await GetProjectFixesAsync(fixAllContext, fixAllContextsAsync).ConfigureAwait(false),
                FixAllScope.Solution => await GetSolutionFixesAsync(fixAllContext, fixAllContextsAsync).ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(fixAllContext.Scope),
            };

            if (solution == null)
                return null;

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'

            return CodeAction.Create(
                title, c => Task.FromResult(solution));

#pragma warning disable RS0005 // Do not use generic 'CodeAction.Create' to create 'CodeAction'
        }

        private static Task<Solution?> GetDocumentFixesAsync(FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
            => fixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext));

        private static Task<Solution?> GetProjectFixesAsync(FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
            => fixAllContextsAsync(fixAllContext, ImmutableArray.Create(fixAllContext.WithDocumentAndProject(document: null, fixAllContext.Project)));

        private static Task<Solution?> GetSolutionFixesAsync(FixAllContext fixAllContext, FixAllContexts fixAllContextsAsync)
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
            return fixAllContextsAsync(
                fixAllContext,
                sortedProjects.SelectAsArray(p => fixAllContext.WithScope(FixAllScope.Project).WithDocumentAndProject(document: null, p)));
        }
    }
}
