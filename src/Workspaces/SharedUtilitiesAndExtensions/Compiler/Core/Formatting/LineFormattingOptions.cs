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
internal sealed record class LineFormattingOptions(
    [property: DataMember(Order = 0)] bool UseTabs = false,
    [property: DataMember(Order = 1)] int TabSize = 4,
    [property: DataMember(Order = 2)] int IndentationSize = 4,
    string? NewLine = null)
{
    [property: DataMember(Order = 3)]
    public string NewLine { get; init; } = NewLine ?? Environment.NewLine;

    public static readonly LineFormattingOptions Default = new();

    public static LineFormattingOptions Create(AnalyzerConfigOptions options, LineFormattingOptions? fallbackOptions)
    {
        fallbackOptions ??= Default;

        return new(
            UseTabs: options.GetEditorConfigOption(FormattingOptions2.UseTabs, fallbackOptions.UseTabs),
            TabSize: options.GetEditorConfigOption(FormattingOptions2.TabSize, fallbackOptions.TabSize),
            IndentationSize: options.GetEditorConfigOption(FormattingOptions2.IndentationSize, fallbackOptions.IndentationSize),
            NewLine: options.GetEditorConfigOption(FormattingOptions2.NewLine, fallbackOptions.NewLine));
    }

#if !CODE_STYLE
    public static async Task<LineFormattingOptions> FromDocumentAsync(Document document, LineFormattingOptions? fallbackOptions, CancellationToken cancellationToken)
    {
        var documentOptions = await document.GetAnalyzerConfigOptionsAsync(cancellationToken).ConfigureAwait(false);
        return Create(documentOptions, fallbackOptions);
    }
#endif
}
