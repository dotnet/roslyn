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
    extension(IOptionsReader options)
    {
        public SimplifierOptions GetSimplifierOptions(Host.LanguageServices languageServices)
        => languageServices.GetService<ISimplificationService>()?.GetSimplifierOptions(options) ?? SimplifierOptions.CommonDefaults;
    }

    extension(Document document)
    {
        public ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(CancellationToken cancellationToken)
        => GetSimplifierOptionsAsync(document, document.GetRequiredLanguageService<ISimplificationService>(), cancellationToken);

        public async ValueTask<SimplifierOptions> GetSimplifierOptionsAsync(ISimplification simplification, CancellationToken cancellationToken)
        {
            var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return simplification.GetSimplifierOptions(configOptions);
        }
    }

    public static SimplifierOptions GetDefault(Host.LanguageServices languageServices)
        => languageServices.GetRequiredService<ISimplificationService>().DefaultOptions;
}

