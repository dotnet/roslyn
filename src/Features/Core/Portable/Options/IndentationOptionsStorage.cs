// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Indentation;

internal static class IndentationOptionsStorage
{
    // TODO: move to LSP layer
    public static async Task<IndentationOptions> GetIndentationOptionsAsync(this IGlobalOptionService globalOptions, Document document, CancellationToken cancellationToken)
    {
        var formattingOptions = await SyntaxFormattingOptions.FromDocumentAsync(document, cancellationToken).ConfigureAwait(false);
        var autoFormattingOptions = globalOptions.GetAutoFormattingOptions(document.Project.Language);
        return new(formattingOptions, autoFormattingOptions);
    }
}
