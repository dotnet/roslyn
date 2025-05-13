// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Capabilities specific to the `textDocument/definition` request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#definitionClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class DefinitionClientCapabilities : DynamicRegistrationSetting
{
    /// <summary>
    /// Whether the client supports supports additional metadata in the form of <see cref="LocationLink"/> definition links
    /// </summary>
    /// <remarks>Since LSP 3.14</remarks>
    [JsonPropertyName("linkSupport")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool LinkSupport { get; init; }
}
