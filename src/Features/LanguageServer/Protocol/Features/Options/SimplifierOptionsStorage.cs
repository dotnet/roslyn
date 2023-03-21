// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Simplification;

internal static class SimplifierOptionsStorage
{
    public static ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetSimplifierOptionsAsync(globalOptions.GetSimplifierOptions(document.Project.Services), cancellationToken);

    public static SimplifierOptions GetSimplifierOptions(this IGlobalOptionService options, LanguageServices languageServices)
        => options.GetSimplifierOptions(languageServices, fallbackOptions: null);
}
