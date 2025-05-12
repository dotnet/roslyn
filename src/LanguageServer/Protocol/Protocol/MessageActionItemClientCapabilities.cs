// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the `MessageActionItem` type
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class MessageActionItemClientCapabilities
{
    /// <summary>
    /// Whether the client supports additional attributes which
    /// are preserved and sent back to the server in the
    /// request's response.
    /// </summary>
    [JsonPropertyName("additionalPropertiesSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool AdditionalPropertiesSupport { get; init; }
}
