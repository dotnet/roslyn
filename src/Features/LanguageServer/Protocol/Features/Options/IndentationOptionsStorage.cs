﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Indentation;

internal static class IndentationOptionsStorage
{
    public static async Task<IndentationOptions> GetIndentationOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
    {
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(globalOptions, cancellationToken).ConfigureAwait(false);

        return new IndentationOptions(formattingOptions)
        {
            AutoFormattingOptions = globalOptions.GetAutoFormattingOptions(document.Project.Language),
            IndentStyle = globalOptions.GetOption(SmartIndent, document.Project.Language)
        };
    }

    public static PerLanguageOption2<FormattingOptions2.IndentStyle> SmartIndent => FormattingOptions2.SmartIndent;
}
