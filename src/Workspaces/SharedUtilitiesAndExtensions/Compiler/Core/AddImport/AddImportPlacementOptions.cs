// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport;

[DataContract]
internal sealed record class AddImportPlacementOptions
{
    public static readonly CodeStyleOption2<AddImportPlacement> s_outsideNamespacePlacementWithSilentEnforcement =
       new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

    [property: DataMember(Order = 0)]
    public bool PlaceSystemNamespaceFirst { get; init; }

    /// <summary>
    /// Where to place C# usings relative to namespace declaration, ignored by VB.
    /// </summary>
    [property: DataMember(Order = 1)]
    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement { get; init; }

    [property: DataMember(Order = 2)]
    public bool AllowInHiddenRegions { get; init; }

    public AddImportPlacementOptions(
        bool PlaceSystemNamespaceFirst = true,
        CodeStyleOption2<AddImportPlacement>? UsingDirectivePlacement = null,
        bool AllowInHiddenRegions = false)
    {
        this.PlaceSystemNamespaceFirst = PlaceSystemNamespaceFirst;
        this.UsingDirectivePlacement = UsingDirectivePlacement ?? s_outsideNamespacePlacementWithSilentEnforcement;
        this.AllowInHiddenRegions = AllowInHiddenRegions;
    }

    public bool PlaceImportsInsideNamespaces => UsingDirectivePlacement.Value == AddImportPlacement.InsideNamespace;

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
