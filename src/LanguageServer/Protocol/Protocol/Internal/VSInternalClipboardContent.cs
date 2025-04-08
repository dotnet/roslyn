// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents content to be sent to the clipboard.
/// </summary>
internal sealed class VSInternalClipboardContent
{
    /// <summary>
    /// Gets or sets a string that describes clipboard format types, for example, "text/plain".
    /// </summary>
    [JsonPropertyName("_vs_mime")]
    [JsonRequired]
    public string MimeType
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the content of the clipboard.
    /// </summary>
    [JsonPropertyName("_vs_content")]
    [JsonRequired]
    public string Content
    {
        get;
        set;
    }
}
