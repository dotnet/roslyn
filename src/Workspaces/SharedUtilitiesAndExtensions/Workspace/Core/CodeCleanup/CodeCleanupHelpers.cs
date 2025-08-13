// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeCleanup;

internal static class CodeCleanupHelpers
{
    public static async Task<Document> CleanupSyntaxAsync(
        Document document, CodeCleanupOptions options, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(document.SupportsSyntaxTree);

        // format any node with explicit formatter annotation
        var syntaxFormatting = document.GetRequiredLanguageService<ISyntaxFormattingService>();
        var document1 = await syntaxFormatting.FormatAsync(document, Formatter.Annotation, options.FormattingOptions, cancellationToken).ConfigureAwait(false);

        // format any elastic whitespace
        var document2 = await syntaxFormatting.FormatAsync(document1, SyntaxAnnotation.ElasticAnnotation, options.FormattingOptions, cancellationToken).ConfigureAwait(false);

        return document2;
    }
}
