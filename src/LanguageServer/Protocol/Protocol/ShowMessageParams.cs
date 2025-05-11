// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// The parameters sent with window/showMessage requests.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showMessage">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal class ShowMessageParams
{
    /// <summary>
    /// Gets or sets the type of message.
    /// </summary>
    [JsonPropertyName("type")]
    [JsonRequired]
    public MessageType MessageType
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    [JsonPropertyName("message")]
    [JsonRequired]
    public string Message
    {
        get;
        set;
    }
}
