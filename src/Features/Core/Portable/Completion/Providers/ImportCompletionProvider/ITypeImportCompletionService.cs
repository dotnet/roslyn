// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
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
        Task<ImmutableArray<ImmutableArray<CompletionItem>>?> GetAllTopLevelTypesAsync(
            Project project,
            SyntaxContext syntaxContext,
            bool forceCacheCreation,
            CancellationToken cancellationToken);
    }
}
