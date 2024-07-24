// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Simplification;

internal static class SimplifierOptionsProviders
{
    public static SimplifierOptions GetSimplifierOptions(this IOptionsReader options, Host.LanguageServices languageServices)
        => languageServices.GetService<ISimplificationService>()?.GetSimplifierOptions(options) ?? SimplifierOptions.CommonDefaults;

    public static ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, CancellationToken cancellationToken)
        => GetSimplifierOptionsAsync(document, document.Project.Services.GetRequiredService<ISimplificationService>(), cancellationToken);

    public static async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(this Document document, ISimplification simplification, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return simplification.GetSimplifierOptions(configOptions);
    }
}

