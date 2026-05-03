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
    /// <summary>
    /// Search for symbols contained in the starting project/compilation.  These are source or metadata symbols that can
    /// be referenced just by adding a using/import.
    /// </summary>
    [DataMember]
    public bool SearchReferencedProjectSymbols { get; init; } = true;

    /// <summary>
    /// Search for source symbols in non-referenced projects.  These are source symbols that can be referenced by adding
    /// a project reference as well as a using/import.
    /// </summary>
    [DataMember]
    public bool SearchUnreferencedProjectSourceSymbols { get; init; } = true;

    /// <summary>
    /// Search for source symbols in non-referenced metadata assemblies (that are referenced by other projects).  These
    /// are source symbols that can be referenced by adding a metadata reference as well as a using/import.
    /// </summary>
    [DataMember]
    public bool SearchUnreferencedMetadataSymbols { get; init; } = true;

    /// <summary>
    /// Search for well known symbols in the common set of .Net reference assemblies.  We have an index for these and
    /// they are common enough to want to always search.
    /// </summary>
    [DataMember]
    public bool SearchReferenceAssemblies { get; init; } = true;

    /// <summary>
    /// Search for symbols in the NuGet package index.
    /// </summary>
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

    public static PerLanguageOption2<bool> SearchReferencedProjectSymbols = new(
        "dotnet_unsupported_search_referenced_project_symbols",
        SymbolSearchOptions.Default.SearchReferencedProjectSymbols,
        isEditorConfigOption: true,
        group: s_optionGroup);

    public static PerLanguageOption2<bool> SearchUnreferencedProjectSourceSymbols = new(
        "dotnet_unsupported_search_unreferenced_project_symbols",
        SymbolSearchOptions.Default.SearchUnreferencedProjectSourceSymbols,
        isEditorConfigOption: true,
        group: s_optionGroup);

    public static PerLanguageOption2<bool> SearchUnreferencedMetadataSymbols = new(
        "dotnet_unsupported_search_unreferenced_metadata_symbols",
        SymbolSearchOptions.Default.SearchUnreferencedMetadataSymbols,
        isEditorConfigOption: true,
        group: s_optionGroup);

    public static readonly ImmutableArray<IOption2> EditorConfigOptions = [SearchReferenceAssemblies];
    public static readonly ImmutableArray<IOption2> UnsupportedOptions = [
        SearchNuGetPackages,
        SearchReferencedProjectSymbols,
        SearchUnreferencedProjectSourceSymbols,
        SearchUnreferencedMetadataSymbols];
}

internal static class SymbolSearchOptionsProviders
{
    internal static SymbolSearchOptions GetSymbolSearchOptions(this IOptionsReader options, string language)
        => new()
        {
            SearchReferenceAssemblies = options.GetOption(SymbolSearchOptionsStorage.SearchReferenceAssemblies, language),
            SearchNuGetPackages = options.GetOption(SymbolSearchOptionsStorage.SearchNuGetPackages, language),
            SearchReferencedProjectSymbols = options.GetOption(SymbolSearchOptionsStorage.SearchReferencedProjectSymbols, language),
            SearchUnreferencedProjectSourceSymbols = options.GetOption(SymbolSearchOptionsStorage.SearchUnreferencedProjectSourceSymbols, language),
            SearchUnreferencedMetadataSymbols = options.GetOption(SymbolSearchOptionsStorage.SearchUnreferencedMetadataSymbols, language),
        };

    public static async ValueTask<SymbolSearchOptions> GetSymbolSearchOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetSymbolSearchOptions(document.Project.Language);
    }
}
