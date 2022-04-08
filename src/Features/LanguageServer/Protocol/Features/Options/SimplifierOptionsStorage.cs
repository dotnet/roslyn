// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification;

internal interface ISimplifierOptionsStorage : ILanguageService
{
    SimplifierOptions GetOptions(IGlobalOptionService globalOptions);
}

internal static class SimplifierOptionsStorage
{
    public static Task<SimplifierOptions> GetSimplifierOptionsAsync(this IGlobalOptionService globalOptions, Document document, CancellationToken cancellationToken)
        => SimplifierOptions.FromDocumentAsync(document, globalOptions.GetFallbackSimplifierOptions(document.Project.LanguageServices), cancellationToken);

    public static SimplifierOptions? GetFallbackSimplifierOptions(this IGlobalOptionService globalOptions, HostLanguageServices languageServices)
        => languageServices.GetService<ISimplifierOptionsStorage>()?.GetOptions(globalOptions);
}
