// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.ComponentModel;
using System.Text.Json.Serialization;

/// <summary>
/// Class which represents configuration values indicating how text documents should be synced.
///
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSyncOptions">Language Server Protocol specification</see> for additional information.
/// </summary>
internal sealed class TextDocumentSyncOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether open and close notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("openClose")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool OpenClose
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the value indicating how text documents are synced with the server.
    /// </summary>
    [JsonPropertyName("change")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [DefaultValue(TextDocumentSyncKind.None)]
    public TextDocumentSyncKind? Change
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether 'will save' notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("willSave")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillSave
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether 'will save until' notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("willSaveWaitUntil")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillSaveWaitUntil
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets a value indicating whether save notifications are sent to the server.
    /// </summary>
    [JsonPropertyName("save")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SumType<bool, SaveOptions>? Save
    {
        get;
        set;
    }
}
