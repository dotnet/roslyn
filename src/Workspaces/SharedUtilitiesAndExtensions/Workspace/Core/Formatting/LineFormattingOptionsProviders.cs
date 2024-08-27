// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class LineFormattingOptionsProviders
{
    public static LineFormattingOptions GetLineFormattingOptions(this IOptionsReader options, string language)
        => new(options, language);

    public static async ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetLineFormattingOptions(document.Project.Language);
    }
}

