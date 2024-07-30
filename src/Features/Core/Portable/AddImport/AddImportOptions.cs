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
    [property: DataMember(Order = 2)] MemberDisplayOptions MemberDisplayOptions);

internal static class AddImportOptionsProviders
{
    public static AddImportOptions GetAddImportOptions(this IOptionsReader options, LanguageServices languageServices, SymbolSearchOptions searchOptions, bool allowImportsInHiddenRegions)
        => new()
        {
            SearchOptions = searchOptions,
            CleanupOptions = options.GetCodeCleanupOptions(languageServices, allowImportsInHiddenRegions),
            MemberDisplayOptions = options.GetMemberDisplayOptions(languageServices.Language)
        };

    public static async ValueTask<AddImportOptions> GetAddImportOptionsAsync(this Document document, SymbolSearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetAddImportOptions(document.Project.Services, searchOptions, document.AllowImportsInHiddenRegions());
    }
}
