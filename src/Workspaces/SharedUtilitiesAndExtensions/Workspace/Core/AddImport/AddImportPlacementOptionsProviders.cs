// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.AddImport;

internal static class AddImportPlacementOptionsProviders
{
    // Normally we don't allow generation into a hidden region in the file.  However, if we have a
    // modern span mapper at our disposal, we do allow it as that host span mapper can handle mapping
    // our edit to their domain appropriate.
    public static bool AllowImportsInHiddenRegions(this Document document)
#if !WORKSPACE
        => AddImportPlacementOptions.Default.AllowInHiddenRegions;
#else
        => document.DocumentServiceProvider.GetService<Host.ISpanMappingService>()?.SupportsMappingImportDirectives == true;
#endif

    public static AddImportPlacementOptions GetAddImportPlacementOptions(this IOptionsReader options, Host.LanguageServices languageServices, bool? allowInHiddenRegions)
        => languageServices.GetRequiredService<IAddImportsService>().GetAddImportOptions(options, allowInHiddenRegions ?? AddImportPlacementOptions.Default.AllowInHiddenRegions);

    public static async ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        var service = document.GetRequiredLanguageService<IAddImportsService>();
        return service.GetAddImportOptions(configOptions, document.AllowImportsInHiddenRegions());
    }
}
