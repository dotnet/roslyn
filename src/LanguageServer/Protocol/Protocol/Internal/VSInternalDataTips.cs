// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// DataTip tag enum.
/// </summary>
[Flags]
internal enum VSInternalDataTipTags
{
    /// <summary>
    /// Data tip range is a linq expression.
    /// </summary>
    LinqExpression = 0x1,
}

/// <summary>
/// Class which represents debugger data tip response.
/// </summary>
internal sealed record VSInternalDataTip
{
    /// <summary>
    /// Gets or sets the value which indicates the applicable hover range within the document.
    /// </summary>
    [JsonPropertyName("_vs_hoverRange")]
    public Range HoverRange { get; init; }

    /// <summary>
    /// Gets or sets the value which indicates the expression's range within the document.
    /// </summary>
    [JsonPropertyName("_vs_expressionRange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Range? ExpressionRange { get; init; }

    /// <summary>
    /// Gets or sets the <see cref="VSInternalDataTipTags"/> for the data tip.
    /// </summary>
    [JsonPropertyName("_vs_dataTipTags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public VSInternalDataTipTags DataTipTags { get; init; }
}
