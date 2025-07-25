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
    extension(IOptionsReader options)
    {
        public LineFormattingOptions GetLineFormattingOptions(string language)
        => new(options, language);
    }

    extension(Document document)
    {
        public async ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(CancellationToken cancellationToken)
        {
            var configOptions = await document.GetHostAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
            return configOptions.GetLineFormattingOptions(document.Project.Language);
        }
    }
}

