// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal interface ITypeImportCompletionService : ILanguageService
{
    /// <summary>
    /// Get completion items for all the accessible top level types from the given project and all its references. 
    /// Each array returned contains all items from one of the reachable entities (i.e. projects and PE references.)
    /// Returns null if we don't have all the items cached and <paramref name="forceCacheCreation"/> is false.
    /// </summary>
    /// <remarks>
    /// Because items from each entity are cached as a separate array, we simply return them as is instead of an 
    /// aggregated array to avoid unnecessary allocations.
    /// </remarks>
    Task<(ImmutableArray<ImmutableArray<CompletionItem>>, bool)> GetAllTopLevelTypesAsync(
        SyntaxContext syntaxContext,
        bool forceCacheCreation,
        CompletionOptions options,
        CancellationToken cancellationToken);

    void QueueCacheWarmUpTask(Project project);
}
