// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

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

internal interface LineFormattingOptionsProvider
#if !CODE_STYLE
    : OptionsProvider<LineFormattingOptions>
#endif
{
}

internal static partial class LineFormattingOptionsProviders
{
    public static LineFormattingOptions GetLineFormattingOptions(this AnalyzerConfigOptions options, LineFormattingOptions? fallbackOptions)
    {
        fallbackOptions ??= LineFormattingOptions.Default;

        return new()
        {
            UseTabs = options.GetEditorConfigOption(FormattingOptions2.UseTabs, fallbackOptions.UseTabs),
            TabSize = options.GetEditorConfigOption(FormattingOptions2.TabSize, fallbackOptions.TabSize),
            IndentationSize = options.GetEditorConfigOption(FormattingOptions2.IndentationSize, fallbackOptions.IndentationSize),
            NewLine = options.GetEditorConfigOption(FormattingOptions2.NewLine, fallbackOptions.NewLine),
        };
    }

#if !CODE_STYLE
    public static async ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, LineFormattingOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var configOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return configOptions.GetLineFormattingOptions(fallbackOptions);
    }

    public static async ValueTask<LineFormattingOptions> GetLineFormattingOptionsAsync(this Document document, LineFormattingOptionsProvider fallbackOptionsProvider, CancellationToken cancellationToken)
        => await GetLineFormattingOptionsAsync(document, await fallbackOptionsProvider.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
#endif
}

