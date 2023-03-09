// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class DocumentFormattingOptionsStorage
{
    public static ValueTask<DocumentFormattingOptions> GetDocumentFormattingOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => document.GetDocumentFormattingOptionsAsync(globalOptions.GetDocumentFormattingOptions(), cancellationToken);

    public static DocumentFormattingOptions GetDocumentFormattingOptions(this IGlobalOptionService globalOptions)
        => globalOptions.GetDocumentFormattingOptions(fallbackOptions: null);
}

