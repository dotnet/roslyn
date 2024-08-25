// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.SymbolSearch;

[DataContract]
internal readonly record struct SymbolSearchOptions()
{
    [DataMember]
    public bool SearchReferenceAssemblies { get; init; } = true;

    [DataMember]
    public bool SearchNuGetPackages { get; init; } = true;

    public static readonly SymbolSearchOptions Default = new();
}

internal static class SymbolSearchOptionsStorage
{
    private static readonly OptionGroup s_optionGroup = new(name: "symbol_search", description: FeaturesResources.Symbol_search);

    public static PerLanguageOption2<bool> SearchReferenceAssemblies = new(
        "dotnet_search_reference_assemblies",
        SymbolSearchOptions.Default.SearchReferenceAssemblies,
        isEditorConfigOption: true,
        group: s_optionGroup);

    public static PerLanguageOption2<bool> SearchNuGetPackages = new(
        "dotnet_unsupported_search_nuget_packages",
        SymbolSearchOptions.Default.SearchNuGetPackages,
        isEditorConfigOption: true,
        group: s_optionGroup);

    public static readonly ImmutableArray<IOption2> EditorConfigOptions = [SearchReferenceAssemblies];
    public static readonly ImmutableArray<IOption2> UnsupportedOptions = [SearchNuGetPackages];
}

internal static class SymbolSearchOptionsProviders
{
    internal static SymbolSearchOptions GetSymbolSearchOptions(this IOptionsReader options, string language)
        => new()
        {
            SearchReferenceAssemblies = options.GetOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies, language),
            SearchNuGetPackages = options.GetOption(SymbolSearchOptionsStorage.SearchNuGetPackages, language)
        };

    public static async ValueTask<SymbolSearchOptions> GetSymbolSearchOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetSymbolSearchOptions(document.Project.Language);
    }
}
