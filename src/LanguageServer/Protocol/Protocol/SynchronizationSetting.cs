// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.LanguageServer.Protocol;

using System.Text.Json.Serialization;

/// <summary>
/// Class which represents synchronization initialization setting.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#textDocumentSyncClientCapabilities">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
internal sealed class SynchronizationSetting : DynamicRegistrationSetting
{
    /// <summary>
    /// Whether the client supports sending <c>textDocument/willSave</c> notifications.
    /// </summary>
    [JsonPropertyName("willSave")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillSave
    {
        get;
        set;
    }

    /// <summary>
    /// Whether the client supports sending <c>textDocument/willSaveWaitUntil</c>
    /// notifications and waits for a response providing text edits which will be
    /// applied to the document before it is saved.
    /// </summary>
    [JsonPropertyName("willSaveWaitUntil")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool WillSaveWaitUntil
    {
        get;
        set;
    }

    /// <summary>
    /// Whether the client supports sending <c>textDocument/didSave</c> notifications.
    /// </summary>
    [JsonPropertyName("didSave")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool DidSave
    {
        get;
        set;
    }
}
