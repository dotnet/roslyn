// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport;

[DataContract]
internal readonly record struct AddImportPlacementOptions(
    [property: DataMember(Order = 0)] bool PlaceSystemNamespaceFirst = true,
    [property: DataMember(Order = 1)] bool PlaceImportsInsideNamespaces = false,
    [property: DataMember(Order = 2)] bool AllowInHiddenRegions = false)
{
    public AddImportPlacementOptions()
        : this(PlaceSystemNamespaceFirst: true)
    {
    }

    public static readonly AddImportPlacementOptions Default = new();
}

internal interface AddImportPlacementOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<AddImportPlacementOptions>
#endif
{
}

#if !CODE_STYLE
internal static partial class AddImportPlacementOptionsProviders
{
    public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, AddImportPlacementOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
        var services = document.Project.Solution.Workspace.Services;
        var configOptions = documentOptions.AsAnalyzerConfigOptions(services.GetRequiredService<Options.IOptionService>(), document.Project.Language);
        var addImportsService = document.GetRequiredLanguageService<IAddImportsService>();

        // Normally we don't allow generation into a hidden region in the file.  However, if we have a
        // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
        // our edit to their domain appropriate.
        var spanMapper = document.Services.GetService<ISpanMappingService>();
        var allowInHiddenRegions = spanMapper != null && spanMapper.SupportsMappingImportDirectives;

        return addImportsService.GetAddImportOptions(configOptions, allowInHiddenRegions, fallbackOptions);
    }

    public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, AddImportPlacementOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetAddImportPlacementOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.LanguageServices, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
}
#endif
