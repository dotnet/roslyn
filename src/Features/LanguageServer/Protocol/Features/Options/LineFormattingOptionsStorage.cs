// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

internal static class LineFormattingOptionsStorage
{
    public static Task<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, IGlobalOptionService globalOptions, CancellationToken cancellationToken)
        => LineFormattingOptions.FromDocumentAsync(document, globalOptions.GetLineFormattingOptions(document.Project.Language), cancellationToken);

    public static LineFormattingOptions GetLineFormattingOptions(this IGlobalOptionService globalOptions, string language)
        => new(
            UseTabs: globalOptions.GetOption(FormattingOptions2.UseTabs, language),
            TabSize: globalOptions.GetOption(FormattingOptions2.TabSize, language),
            IndentationSize: globalOptions.GetOption(FormattingOptions2.IndentationSize, language),
            NewLine: globalOptions.GetOption(FormattingOptions2.NewLine, language));
}

