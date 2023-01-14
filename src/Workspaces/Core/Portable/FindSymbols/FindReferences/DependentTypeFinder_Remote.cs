// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        public static async Task<ImmutableArray<INamedTypeSymbol>> FindTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project>? projects,
            bool transitive,
            DependentTypesKind kind,
            CancellationToken cancellationToken)
        {
            if (SerializableSymbolAndProjectId.TryCreate(type, solution, cancellationToken, out var serializedType))
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var projectIds = projects?.Where(p => RemoteSupportedLanguages.IsSupported(p.Language)).SelectAsArray(p => p.Id) ?? default;

                    var result = await client.TryInvokeAsync<IRemoteDependentTypeFinderService, ImmutableArray<SerializableSymbolAndProjectId>>(
                        solution,
                        (service, solutionInfo, cancellationToken) => service.FindTypesAsync(solutionInfo, serializedType, projectIds, transitive, kind, cancellationToken),
                        cancellationToken).ConfigureAwait(false);

                    if (!result.HasValue)
                    {
                        return ImmutableArray<INamedTypeSymbol>.Empty;
                    }

                    return await RehydrateAsync(solution, result.Value, cancellationToken).ConfigureAwait(false);
                }

                // TODO: Do not fall back to in-proc https://github.com/dotnet/roslyn/issues/47557
            }

            return await FindTypesInCurrentProcessAsync(type, solution, projects, transitive, kind, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<INamedTypeSymbol>> FindTypesInCurrentProcessAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project>? projects,
            bool transitive,
            DependentTypesKind kind,
            CancellationToken cancellationToken)
        {
            var functionId = kind switch
            {
                DependentTypesKind.DerivedClasses => FunctionId.DependentTypeFinder_FindAndCacheDerivedClassesAsync,
                DependentTypesKind.DerivedInterfaces => FunctionId.DependentTypeFinder_FindAndCacheDerivedInterfacesAsync,
                DependentTypesKind.ImplementingTypes => FunctionId.DependentTypeFinder_FindAndCacheImplementingTypesAsync,
                _ => throw ExceptionUtilities.UnexpectedValue(kind)
            };

            using (Logger.LogBlock(functionId, cancellationToken))
            {
                var task = kind switch
                {
                    DependentTypesKind.DerivedClasses => FindDerivedClassesInCurrentProcessAsync(type, solution, projects, transitive, cancellationToken),
                    DependentTypesKind.DerivedInterfaces => FindDerivedInterfacesInCurrentProcessAsync(type, solution, projects, transitive, cancellationToken),
                    DependentTypesKind.ImplementingTypes => FindImplementingTypesInCurrentProcessAsync(type, solution, projects, transitive, cancellationToken),
                    _ => throw ExceptionUtilities.UnexpectedValue(kind)
                };

                return await task.ConfigureAwait(false);
            }
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> RehydrateAsync(Solution solution, ImmutableArray<SerializableSymbolAndProjectId> values, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<INamedTypeSymbol>.GetInstance(out var builder);

            foreach (var item in values)
            {
                var rehydrated = await item.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (rehydrated is INamedTypeSymbol namedType)
                    builder.AddIfNotNull(namedType);
            }

            return builder.ToImmutable();
        }
    }
}
