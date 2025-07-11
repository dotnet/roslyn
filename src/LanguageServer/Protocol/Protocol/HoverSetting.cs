// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Client capabilities specific to the `textDocument/hover` request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#hoverClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class HoverSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// The client supports the following content formats in a <see cref="MarkupContent"/>
    /// instance in <see cref="Hover.Contents"/>.
    /// <para>
    /// The order describes the preferred format of the client.
    /// </para>
    /// </summary>
    [JsonPropertyName("contentFormat")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkupKind[]? ContentFormat
    {
        get;
        set;
    }
}
