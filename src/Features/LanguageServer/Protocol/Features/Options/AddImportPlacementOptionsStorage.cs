// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.AddImport;

internal interface IAddImportPlacementOptionsStorage : ILanguageService
{
    AddImportPlacementOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class AddImportPlacementOptionsStorage
{
    public static ValueTask<AddImportPlacementOptions> GetAddImportPlacementOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetAddImportPlacementOptionsAsync(globalOptions.GetAddImportPlacementOptions(document.Project.LanguageServices), cancellationToken);

    public static AddImportPlacementOptions GetAddImportPlacementOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => languageServices.GetRequiredService<IAddImportPlacementOptionsStorage>().GetOptions(globalOptions);
}
