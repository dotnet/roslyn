// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : IAdvancedNavigateToSearchService
    {
        public IImmutableSet<string> KindsProvided { get; } = ImmutableHashSet.Create(
            NavigateToItemKind.Class,
            NavigateToItemKind.Constant,
            NavigateToItemKind.Delegate,
            NavigateToItemKind.Enum,
            NavigateToItemKind.EnumItem,
            NavigateToItemKind.Event,
            NavigateToItemKind.Field,
            NavigateToItemKind.Interface,
            NavigateToItemKind.Method,
            NavigateToItemKind.Module,
            NavigateToItemKind.Property,
            NavigateToItemKind.Structure);

        public bool CanFilter => true;

        private static Func<RoslynNavigateToItem, Task> GetOnItemFoundCallback(
            Solution solution, Document? activeDocument, Func<Project, INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken)
        {
            return async item =>
            {
                // This must succeed.  We should always be searching for items that correspond to documents/projects in
                // the host side solution.  Note: this even includes 'cached' items.  While those may correspond to
                // stale versions of a document, it should still be for documents that the host has asked about.
                var project = solution.GetRequiredProject(item.DocumentId.ProjectId);

                var result = await item.TryCreateSearchResultAsync(solution, activeDocument, cancellationToken).ConfigureAwait(false);
                if (result != null)
                    await onResultFound(project, result).ConfigureAwait(false);
            };
        }

        private static PooledDisposer<PooledHashSet<T>> GetPooledHashSet<T>(IEnumerable<T> items, out PooledHashSet<T> instance)
        {
            var disposer = PooledHashSet<T>.GetInstance(out instance);
            instance.AddRange(items);
            return disposer;
        }

        private static PooledDisposer<PooledHashSet<T>> GetPooledHashSet<T>(ImmutableArray<T> items, out PooledHashSet<T> instance)
        {
            var disposer = PooledHashSet<T>.GetInstance(out instance);
            instance.AddRange(items);
            return disposer;
        }
    }
}
