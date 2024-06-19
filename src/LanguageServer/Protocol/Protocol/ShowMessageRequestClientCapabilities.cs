// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the 'window/showMessage' request
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showMessageRequest">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class ShowMessageRequestClientCapabilities
{
    /// <summary>
    /// Capabilities specific to the `MessageActionItem` type
    /// </summary>
    [JsonPropertyName("messageActionItem")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MessageActionItemClientCapabilities? MessageActionItem { get; init; }
}
