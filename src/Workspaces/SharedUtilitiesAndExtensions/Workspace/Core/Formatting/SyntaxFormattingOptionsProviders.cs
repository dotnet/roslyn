// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class SyntaxFormattingOptionsProviders
{
    public static SyntaxFormattingOptions GetSyntaxFormattingOptions(this IOptionsReader options, Host.LanguageServices languageServices)
        => languageServices.GetRequiredService<ISyntaxFormattingService>().GetFormattingOptions(options);

    public static ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, CancellationToken cancellationToken)
        => GetSyntaxFormattingOptionsAsync(document, document.GetRequiredLanguageService<ISyntaxFormattingService>(), cancellationToken);

    public static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, ISyntaxFormatting formatting, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return formatting.GetFormattingOptions(configOptions);
    }
}
