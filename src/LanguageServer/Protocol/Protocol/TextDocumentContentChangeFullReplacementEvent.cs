// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

internal sealed class TextDocumentContentChangeFullReplacementEvent
{
    /// <summary>
    /// Gets or sets the new text of the document.
    /// </summary>
    [JsonPropertyName("text")]
    [JsonRequired]
    public string Text
    {
        get;
        set;
    }
}
