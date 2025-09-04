// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class representing a previous result id in a 'workspace/diagnostic' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#previousResultId">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal sealed class PreviousResultId
{
    /// <summary>
    /// Gets or sets the URI for which the client knows a result id.
    /// </summary>
    [JsonPropertyName("uri")]
    [JsonConverter(typeof(DocumentUriConverter))]
    public DocumentUri Uri
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value of the previous result id.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value
    {
        get;
        set;
    }
}
