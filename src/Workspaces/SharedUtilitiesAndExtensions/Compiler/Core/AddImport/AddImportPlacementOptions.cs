// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;

#if !CODE_STYLE
using Microsoft.CodeAnalysis.Host;
#endif

namespace Microsoft.CodeAnalysis.AddImport;

[DataContract]
internal sealed record class AddImportPlacementOptions
{
    public static readonly CodeStyleOption2<AddImportPlacement> s_outsideNamespacePlacementWithSilentEnforcement =
       new(AddImportPlacement.OutsideNamespace, NotificationOption2.Silent);

    [DataMember]
    public bool PlaceSystemNamespaceFirst { get; init; } = true;

    /// <summary>
    /// Where to place C# usings relative to namespace declaration, ignored by VB.
    /// </summary>
    [DataMember]
    public CodeStyleOption2<AddImportPlacement> UsingDirectivePlacement { get; init; } = s_outsideNamespacePlacementWithSilentEnforcement;

    [DataMember]
    public bool AllowInHiddenRegions { get; init; } = false;

    public bool PlaceImportsInsideNamespaces => UsingDirectivePlacement.Value == AddImportPlacement.InsideNamespace;

    public static readonly AddImportPlacementOptions Default = new();
}

internal interface AddImportPlacementOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<AddImportPlacementOptions>
#endif
{
}

internal static partial class AddImportPlacementOptionsProviders
{
#if !CODE_STYLE
    public static AddImportPlacementOptions GetAddImportPlacementOptions(this IOptionsReader options, LanguageServices languageServices, bool? allowInHiddenRegions, AddImportPlacementOptions? fallbackOptions)
        => languageServices.GetRequiredService<IAddImportsService>().GetAddImportOptions(options, allowInHiddenRegions ?? AddImportPlacementOptions.Default.AllowInHiddenRegions, fallbackOptions);

    public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, AddImportPlacementOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetAddImportPlacementOptions(document.Project.Services, document.AllowImportsInHiddenRegions(), fallbackOptions);
    }

    // Normally we don't allow generation into a hidden region in the file.  However, if we have a
    // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
    // our edit to their domain appropriate.
    public static bool AllowImportsInHiddenRegions(this Document document)
        => document.Services.GetService<ISpanMappingService>()?.SupportsMappingImportDirectives == true;

    public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, AddImportPlacementOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetAddImportPlacementOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
}
