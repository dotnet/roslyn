// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to <see cref="FoldingRange"/>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#foldingRangeClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class FoldingRangeSettingOptions
{
    /// <summary>
    /// If set, the client signals that it supports setting <see cref="FoldingRange.CollapsedText"/>
    /// to display custom labels instead of the default text.
    /// </summary>
    [JsonPropertyName("collapsedText")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CollapsedText
    {
        get;
        set;
    }
}
