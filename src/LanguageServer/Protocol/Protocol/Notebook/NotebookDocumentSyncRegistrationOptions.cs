// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Subclass of <see cref="NotebookDocumentSyncOptions"/> that implements
/// <see cref="IStaticRegistrationOptions"/>, allowing it to be registered
/// with an ID that can be used to unregister it later.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#notebookDocumentSyncRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.17</remarks>
internal class NotebookDocumentSyncRegistrationOptions : NotebookDocumentSyncOptions, IStaticRegistrationOptions
{
    /// <summary>
    /// The id used to register the request. The id can be used to deregister the request again.
    /// </summary>
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }
}
