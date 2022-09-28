// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    [DataContract]
    internal sealed class UnitTestingSearchQuery
    {
        [DataMember(Order = 0)]
        public readonly string FullyQualifiedTypeName;
        [DataMember(Order = 1)]
        public readonly string? MethodName;
        [DataMember(Order = 2)]
        public readonly int MethodArity;
        [DataMember(Order = 3)]
        public readonly int MethodParameterCount;

        public static UnitTestingSearchQuery ForType(string fullyQualifiedTypeName)
            => new(fullyQualifiedTypeName, methodName: null, methodArity: 0, methodParameterCount: 0);

        public static UnitTestingSearchQuery ForMethod(string fullyQualifiedTypeName, string methodName, int methodArity, int methodParameterCount)
            => new(fullyQualifiedTypeName, methodName, methodArity, methodParameterCount);

        private UnitTestingSearchQuery(string fullyQualifiedTypeName, string methodName, int methodArity, int methodParameterCount)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = methodName;
            MethodArity = methodArity;
            MethodParameterCount = methodParameterCount;
        }
    }

    internal readonly record struct UnitTestingNavigationOptions(
        bool PreferProvisionalTab = false,
        bool ActivateTab = true)
    {
        public UnitTestingNavigationOptions()
            : this(PreferProvisionalTab: false)
        {
        }
    }

    internal readonly struct UnitTestingDocumentSpan
    {
        private readonly DocumentSpan _documentSpan;

        internal UnitTestingDocumentSpan(DocumentSpan documentSpan, FileLinePositionSpan span)
        {
            _documentSpan = documentSpan;
            Span = span;
        }

        public FileLinePositionSpan Span { get; }

        public async Task NavigateToAsync(UnitTestingNavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await _documentSpan.GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
            if (location != null)
                await location.NavigateToAsync(new NavigationOptions(options.PreferProvisionalTab, options.ActivateTab), cancellationToken).ConfigureAwait(false);
        }

        internal static class UnitTestingSearchHelpers
        {
            public static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsAsync(
                Solution solution, UnitTestingSearchQuery query, CancellationToken cancellationToken)
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);

                if (client != null)
                {
                    var locations = await client.TryInvokeAsync<IRemoteUnitTestingSearchService, ImmutableArray<UnitTestingSourceLocation>>(
                        solution,
                        (service, solutionChecksum, cancellationToken) => service.GetSourceLocationsAsync(solutionChecksum, query, cancellationToken),
                        cancellationToken).ConfigureAwait(false);
                    if (!locations.HasValue)
                        return ImmutableArray<UnitTestingDocumentSpan>.Empty;

                    using var _ = ArrayBuilder<UnitTestingDocumentSpan>.GetInstance(out var result);
                    foreach (var location in locations.Value)
                        result.AddIfNotNull(await location.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false));

                    return result.ToImmutable();
                }

                return await GetSourceLocationsInProcessAsync(solution, query, cancellationToken).ConfigureAwait(false);
            }

            private static Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsInProcessAsync(
                Solution solution,
                UnitTestingSearchQuery query,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
