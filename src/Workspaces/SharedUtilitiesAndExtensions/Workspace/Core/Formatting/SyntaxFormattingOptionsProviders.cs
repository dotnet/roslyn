// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Formatting;

internal static partial class SyntaxFormattingOptionsProviders
{
    internal static async ValueTask<SyntaxFormattingOptions> GetSyntaxFormattingOptionsAsync(this Document document, ISyntaxFormatting syntaxFormatting, SyntaxFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
    {
#if CODE_STYLE
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        return syntaxFormatting.GetFormattingOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
#else
        return await document.GetSyntaxFormattingOptionsAsync(fallbackOptionsProvider, cancellationToken).ConfigureAwait(false);
#endif
    }

#if CODE_STYLE
#pragma warning disable IDE0060 // Fallback options currently unused in code style fixers
    public static async ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, LineFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
#pragma warning restore
    {
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        return LineFormattingOptions.Create(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree), fallbackOptions: null);
    }
#endif
}
