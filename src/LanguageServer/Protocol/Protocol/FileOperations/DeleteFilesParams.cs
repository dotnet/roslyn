// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The parameters sent in notifications/requests for user-initiated deletes of files.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#deleteFilesParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class DeleteFilesParams
{
    /// <summary>
    /// An array of all files/folders deleted in this operation.
    /// </summary>
    [JsonPropertyName("files")]
    [JsonRequired]
    public FileCreate[] Files { get; init; }
}
