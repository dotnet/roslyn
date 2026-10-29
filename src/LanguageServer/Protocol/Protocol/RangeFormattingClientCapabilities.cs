// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Capabilities specific to the `textDocument/rangeFormatting` request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentRangeFormattingClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class RangeFormattingClientCapabilities : DynamicRegistrationSetting
{
    /// <summary>
    /// Whether the client supports formatting multiple ranges at once.
    /// </summary>
    /// <remarks>Since LSP 3.18</remarks>
    [JsonPropertyName("rangesSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool RangesSupport { get; init; }
}
