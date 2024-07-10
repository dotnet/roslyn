// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Formatting;

[DataContract]
internal sealed record class LineFormattingOptions
{
    public static readonly LineFormattingOptions Default = new();

    [DataMember] public bool UseTabs { get; init; } = false;
    [DataMember] public int TabSize { get; init; } = 4;
    [DataMember] public int IndentationSize { get; init; } = 4;
    [DataMember] public string NewLine { get; init; } = Environment.NewLine;
}

internal static partial class LineFormattingOptionsProviders
{
    public static LineFormattingOptions GetLineFormattingOptions(this IOptionsReader options, string language)
        => new()
        {
            UseTabs = options.GetOption(FormattingOptions2.UseTabs, language),
            TabSize = options.GetOption(FormattingOptions2.TabSize, language),
            IndentationSize = options.GetOption(FormattingOptions2.IndentationSize, language),
            NewLine = options.GetOption(FormattingOptions2.NewLine, language),
        };

#if !CODE_STYLE
    public static async ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetLineFormattingOptions(document.Project.Language);
    }
#endif
}

