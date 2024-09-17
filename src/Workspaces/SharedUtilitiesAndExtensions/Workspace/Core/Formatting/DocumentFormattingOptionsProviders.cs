// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class DocumentFormattingOptionsProviders
{
    public static DocumentFormattingOptions GetDocumentFormattingOptions(this IOptionsReader options)
        => new()
        {
            FileHeaderTemplate = options.GetOption(CodeStyleOptions2.FileHeaderTemplate),
            InsertFinalNewLine = options.GetOption(FormattingOptions2.InsertFinalNewLine)
        };

    public static async ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetDocumentFormattingOptions();
    }
}
