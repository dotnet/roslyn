// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the `textDocument/documentLink` request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#documentLinkClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DocumentLinkClientCapabilities : DynamicRegistrationSetting
{
    /// <summary>
    /// Whether the client supports the <see cref="DocumentLink.Tooltip"/> property.
    /// </summary>
    /// <remarks>Since LSP 3.15</remarks>
    [JsonPropertyName("tooltipSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool TooltipSupport { get; init; }
}
