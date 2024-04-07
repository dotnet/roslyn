// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal static class UnitTestingSearchHelpers
{
    private static readonly char[] s_splitCharacters = ['.', '+'];

    public static async Task<UnitTestingDocumentSpan?> GetSourceLocationAsync(
        Project project, UnitTestingSearchQuery query, CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return null;

        var client = await RemoteHostClient.TryGetClientAsync(project.Solution.Services, cancellationToken).ConfigureAwait(false);

        if (client != null)
        {
            var location = await client.TryInvokeAsync<IRemoteUnitTestingSearchService, UnitTestingSourceLocation?>(
                project,
                (service, solutionChecksum, cancellationToken) => service.GetSourceLocationAsync(solutionChecksum, project.Id, query, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!location.HasValue || location.Value is null)
                return null;

            return await location.Value.Value.TryRehydrateAsync(project.Solution, cancellationToken).ConfigureAwait(false);
        }

        return await GetSourceLocationInProcessAsync(project, query, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsAsync(
        Project project, UnitTestingSearchQuery query, CancellationToken cancellationToken)
    {
        if (!project.SupportsCompilation)
            return [];

        var client = await RemoteHostClient.TryGetClientAsync(project.Solution.Services, cancellationToken).ConfigureAwait(false);

        if (client != null)
        {
            var locations = await client.TryInvokeAsync<IRemoteUnitTestingSearchService, ImmutableArray<UnitTestingSourceLocation>>(
                project,
                (service, solutionChecksum, cancellationToken) => service.GetSourceLocationsAsync(solutionChecksum, project.Id, query, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            if (!locations.HasValue)
                return [];

            using var _ = ArrayBuilder<UnitTestingDocumentSpan>.GetInstance(out var result);
            foreach (var location in locations.Value)
                result.AddIfNotNull(await location.TryRehydrateAsync(project.Solution, cancellationToken).ConfigureAwait(false));

            return result.ToImmutable();
        }

        return await GetSourceLocationsInProcessAsync(project, query, cancellationToken).ConfigureAwait(false);
    }

    private static (string containerName, string symbolName, int symbolArity) ExtractQueryData(UnitTestingSearchQuery query)
    {
        if (query.MethodName == null)
        {
            // if we don't have a method name, then the fully qualified type name needs to be broken into two parts:
            // 1. the name of the type symbol we're looking for (the last name portion of the qualified name)
            // 2. the container of the type symbol we're looking for (all but the last name portion).
            var fullyQualifiedTypeName = query.FullyQualifiedTypeName;
            var lastPlus = fullyQualifiedTypeName.LastIndexOf('+');
            var lastDot = fullyQualifiedTypeName.LastIndexOf('.');

            var (container, type) =
                lastPlus >= 0 ? (fullyQualifiedTypeName[..lastPlus], fullyQualifiedTypeName[(lastPlus + 1)..]) :
                lastDot >= 0 ? (fullyQualifiedTypeName[..lastDot], fullyQualifiedTypeName[(lastDot + 1)..]) :
                ("", fullyQualifiedTypeName);

            GetNameAndArity(type, out type, out var typeArity);

            return (ConvertFromMetadataTypeName(container), type, typeArity);
        }
        else
        {
            // If we have a method name, then that's the name we'll search in the index for. The fully qualified
            // type name is what we'll use to check the container of any methods we find.
            return (ConvertFromMetadataTypeName(query.FullyQualifiedTypeName), query.MethodName, query.MethodArity);
        }
    }

    /// <summary>
    /// Converts from a metadata-name into the internal simple dotted name we store in our index as the container
    /// for a symbol.  In the future, we could consider storing the fully-qualified-metadata-name in our index as
    /// it's trivial to compute it as we're walking down the syntax tree.
    /// </summary>
    private static string ConvertFromMetadataTypeName(string fullyQualifiedTypeName)
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
            int.TryParse(typeName[(backtickIndex + 1)..], NumberStyles.None, CultureInfo.InvariantCulture, out typeArity);
        }
    }

    private static async Task<UnitTestingDocumentSpan?> GetSourceLocationInProcessAsync(
        Project project,
        UnitTestingSearchQuery query,
        CancellationToken cancellationToken)
    {
        // Return the first result we find.
        await foreach (var item in GetSourceLocationsInProcessWorkerAsync(project, query, cancellationToken).ConfigureAwait(false))
            return item;

        return null;
    }

    private static async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsInProcessAsync(
        Project project,
        UnitTestingSearchQuery query,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<UnitTestingDocumentSpan>.GetInstance(out var result);

        await foreach (var item in GetSourceLocationsInProcessWorkerAsync(project, query, cancellationToken).ConfigureAwait(false))
            result.Add(item);

        return result.ToImmutable();
    }

    private static IAsyncEnumerable<UnitTestingDocumentSpan> GetSourceLocationsInProcessWorkerAsync(
        Project project,
        UnitTestingSearchQuery query,
        CancellationToken cancellationToken)
    {
        var (container, symbolName, symbolArity) = ExtractQueryData(query);
        var syntaxFacts = project.GetRequiredLanguageService<ISyntaxFactsService>();
        var comparer = syntaxFacts.StringComparer;

        var streams = project.Documents.SelectAsArray(d => GetSourceLocationsInProcessAsync(d, comparer, container, symbolName, symbolArity, query, cancellationToken));
        return streams.MergeAsync(cancellationToken);
    }

    private static async IAsyncEnumerable<UnitTestingDocumentSpan> GetSourceLocationsInProcessAsync(
        Document document,
        StringComparer comparer,
        string container,
        string symbolName,
        int symbolArity,
        UnitTestingSearchQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // quick check that the symbol name is actually in the bloom-filter index for this document.  Can avoid
        // checking any of the items in it if so.
        var syntaxTreeIndex = await SyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
        if (!syntaxTreeIndex.ProbablyContainsIdentifier(symbolName))
            yield break;

        // Ok, the symbol name was in this document.  Now go get the declaration-index and look through that to find
        // the location of a potential matching symbol.

        SyntaxTree? tree = null;

        // Walk each of the top-level-index infos we've got for this tree.
        var index = await TopLevelSyntaxTreeIndex.GetRequiredIndexAsync(document, cancellationToken).ConfigureAwait(false);
        foreach (var info in index.DeclaredSymbolInfos)
        {
            // Fast checks first to see if this looks like a candidate.

            // In non-strict mode, allow the type-parameter count to be mismatched.
            if (query.Strict && info.TypeParameterCount != symbolArity)
                continue;

            if (query.MethodName != null)
            {
                // We're searching for unit test methods.  Those must always have an attribute on them of some sort.
                if (!info.HasAttributes)
                    continue;

                // Has to actually be a method.
                if (info.Kind is not (DeclaredSymbolInfoKind.Method or DeclaredSymbolInfoKind.ExtensionMethod))
                    continue;

                // In non-strict mode, allow the parameter count to be mismatched.
                if (query.Strict && info.ParameterCount != query.MethodParameterCount)
                    continue;
            }

            // Looking promising so far.  Check that the names matches what the caller needs.
            if (!comparer.Equals(info.Name, symbolName))
                continue;

            if (!comparer.Equals(info.FullyQualifiedContainerName, container))
                continue;

            // Unit testing needs to know the final span a location may be mapped to (e.g. with `#line` taken
            // into consideration).  So map that information here for them.
            tree ??= await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var mappedSpan = tree.GetMappedLineSpan(info.Span, cancellationToken);

            yield return new UnitTestingDocumentSpan(new DocumentSpan(document, info.Span), mappedSpan);
        }
    }
}
