// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The parameters sent in notifications/requests for user-initiated creation of files.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#createFilesParams">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class CreateFilesParams
{
    /// <summary>
    /// An array of all files/folders created in this operation.
    /// </summary>
    [JsonPropertyName("files")]
    [JsonRequired]
    public FileCreate[] Files { get; init; }
}
