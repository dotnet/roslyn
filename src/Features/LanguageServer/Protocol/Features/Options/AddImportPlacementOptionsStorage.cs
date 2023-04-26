// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.AddImport;

internal static class AddImportPlacementOptionsStorage
{
    public static ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetAddImportPlacementOptionsAsync(globalOptions.GetAddImportPlacementOptions(document.Project.Services), cancellationToken);

    public static AddImportPlacementOptions GetAddImportPlacementOptions(this IGlobalOptionService globalOptions, LanguageServices languageServices)
        => globalOptions.GetAddImportPlacementOptions(languageServices, allowInHiddenRegions: null, fallbackOptions: null);
}
