// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SymbolSearch;

namespace Microsoft.CodeAnalysis.AddImport;

[DataContract]
internal readonly record struct AddImportOptions(
    [property: DataMember(Order = 0)] SymbolSearchOptions SearchOptions,
    [property: DataMember(Order = 1)] CodeCleanupOptions CleanupOptions,
    [property: DataMember(Order = 2)] MemberDisplayOptions MemberDisplayOptions,
    [property: DataMember(Order = 3)] bool CleanupDocument);

internal static class AddImportOptionsProviders
{
    public static AddImportOptions GetAddImportOptions(
        this IOptionsReader options,
        LanguageServices languageServices,
        SymbolSearchOptions searchOptions,
        bool allowImportsInHiddenRegions,
        bool cleanupDocument)
        => new()
        {
            SearchOptions = searchOptions,
            CleanupOptions = options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions),
            MemberDisplayOptions = options.GetMemberDisplayOptions(languageServices.Language),
            CleanupDocument = cleanupDocument,
        };

    public static async ValueTask<AddImportOptions> GetAddImportOptionsAsync(
        this Document document,
        SymbolSearchOptions searchOptions,
        bool cleanupDocument,
        CancellationToken cancellationToken)
    {
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetAddImportOptions(
            document.Project.Services, searchOptions, document.AllowImportsInHiddenRegions(), cleanupDocument);
    }
}
