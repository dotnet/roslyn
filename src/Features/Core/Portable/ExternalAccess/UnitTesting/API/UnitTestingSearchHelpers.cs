// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;

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

        private UnitTestingSearchQuery(string fullyQualifiedTypeName, string? methodName, int methodArity, int methodParameterCount)
        {
            FullyQualifiedTypeName = fullyQualifiedTypeName;
            MethodName = methodName;
            MethodArity = methodArity;
            MethodParameterCount = methodParameterCount;
        }
    }

    internal static class UnitTestingSearchHelpers
    {
        private static readonly char[] s_splitCharacters = { '.', '+' };

        public static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsAsync(
            Project project, UnitTestingSearchQuery query, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
                return ImmutableArray<UnitTestingDocumentSpan>.Empty;

            var client = await RemoteHostClient.TryGetClientAsync(project.Solution.Services, cancellationToken).ConfigureAwait(false);

            if (client != null)
            {
                var locations = await client.TryInvokeAsync<IRemoteUnitTestingSearchService, ImmutableArray<UnitTestingSourceLocation>>(
                    project,
                    (service, solutionChecksum, cancellationToken) => service.GetSourceLocationsAsync(solutionChecksum, project.Id, query, cancellationToken),
                    cancellationToken).ConfigureAwait(false);
                if (!locations.HasValue)
                    return ImmutableArray<UnitTestingDocumentSpan>.Empty;

                using var _ = ArrayBuilder<UnitTestingDocumentSpan>.GetInstance(out var result);
                foreach (var location in locations.Value)
                    result.AddIfNotNull(await location.TryRehydrateAsync(project.Solution, cancellationToken).ConfigureAwait(false));

                return result.ToImmutable();
            }

            return await GetSourceLocationsInProcessAsync(project, query, cancellationToken).ConfigureAwait(false);
        }

        private static (string containerName, string symbolName, int symbolArity) BuildQueryInfo(UnitTestingSearchQuery query)
        {
            if (query.MethodName == null)
            {
                var fullyQualifiedTypeName = query.FullyQualifiedTypeName;
                var lastPlus = fullyQualifiedTypeName.LastIndexOf('+');
                var lastDot = fullyQualifiedTypeName.LastIndexOf('.');

                var (container, type) =
                    lastPlus >= 0 ? (fullyQualifiedTypeName[..lastPlus], fullyQualifiedTypeName[(lastPlus + 1)..]) :
                    lastDot >= 0 ? (fullyQualifiedTypeName[..lastDot], fullyQualifiedTypeName[(lastDot + 1)..]) :
                    ("", fullyQualifiedTypeName);

                GetNameAndArity(type, out type, out var typeArity);

                return (ConvertTypeName(container), type, typeArity);
            }
            else
            {
                return (ConvertTypeName(query.FullyQualifiedTypeName), query.MethodName, query.MethodArity);
            }
        }

        private static string ConvertTypeName(string fullyQualifiedTypeName)
        {
            var pieces = fullyQualifiedTypeName.Split(s_splitCharacters);
            using var _ = PooledStringBuilder.GetInstance(out var result);

            foreach (var piece in pieces)
            {
                GetNameAndArity(piece, out var pieceWithoutArity, out var _);
                if (result.Length > 0)
                    result.Append('.');

                result.Append(pieceWithoutArity);
            }

            return result.ToString();
        }

        private static void GetNameAndArity(string typeName, out string typeNameWithoutArity, out int typeArity)
        {
            var backtickIndex = typeName.LastIndexOf('`');
            if (backtickIndex < 0)
            {
                typeNameWithoutArity = typeName;
                typeArity = 0;
            }
            else
            {
                typeNameWithoutArity = typeName[0..backtickIndex];
                typeArity = int.Parse(typeName[(backtickIndex + 1)..], CultureInfo.InvariantCulture);
            }
        }

        private static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsInProcessAsync(
            Project project,
            UnitTestingSearchQuery query,
            CancellationToken cancellationToken)
        {
            var (container, symbolName, symbolArity) = BuildQueryInfo(query);

            var tasks = project.Documents.Select(d => GetSourceLocationsInProcessAsync(d, container, symbolName, symbolArity, query, cancellationToken));
            var result = await Task.WhenAll(tasks).ConfigureAwait(false);
            return result.SelectMany(r => r).ToImmutableArray();
        }

        private static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsInProcessAsync(
            Document document,
            string container,
            string symbolName,
            int symbolArity,
            UnitTestingSearchQuery query,
            CancellationToken cancellationToken)
        {
            var index = await TopLevelSyntaxTreeIndex.GetIndexAsync(document, cancellationToken).ConfigureAwait(false);
            if (index == null)
                return ImmutableArray<UnitTestingDocumentSpan>.Empty;

            using var _ = ArrayBuilder<UnitTestingDocumentSpan>.GetInstance(out var result);

            SyntaxTree? tree = null;

            foreach (var info in index.DeclaredSymbolInfos)
            {
                if (info.Name == symbolName &&
                    info.TypeParameterCount == symbolArity)
                {
                    // If it's a method, the parameter count much match.
                    if (query.MethodName != null &&
                        info.ParameterCount != query.MethodParameterCount)
                    {
                        continue;
                    }

                    if (info.FullyQualifiedContainerName != container)
                        continue;

                    tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    var mappedSpan = tree.GetMappedLineSpan(info.Span, cancellationToken);

                    result.Add(new UnitTestingDocumentSpan(new DocumentSpan(document, info.Span), mappedSpan));
                }
            }

            return result.ToImmutable();
        }
    }
}
