// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
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
                var project = solution.GetProject(item.DocumentId.ProjectId);
                if (project is null)
                    return;

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
