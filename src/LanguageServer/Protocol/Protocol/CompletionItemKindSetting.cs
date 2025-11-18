// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Describes the <see cref="CompletionItemKind"/> values supported by the client
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#completionClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class CompletionItemKindSetting
{
    /// <summary>
    /// Gets or sets the <see cref="CompletionItemKind"/> values that the client supports.
    /// <para>
    /// The completion item kind values the client supports. When this
    /// property exists the client also guarantees that it will
    /// handle values outside its set gracefully and falls back
    /// to a default value when unknown.
    /// </para>
    /// <para>
    /// If this property is not present the client only supports the completion item
    /// kinds from <see cref="CompletionItemKind.Text"/> to <see cref="CompletionItemKind.Reference"/>
    /// as defined in the initial version of the protocol.
    /// </para>
    /// </summary>
    [JsonPropertyName("valueSet")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionItemKind[]? ValueSet
    {
        get;
        set;
    }
}
