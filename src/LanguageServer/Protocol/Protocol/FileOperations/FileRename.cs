// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Represents information on a file/folder rename.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileRename">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal class FileRename
{
    /// <summary>
    /// A <c>file://</c> URI for the original location of the file/folder being renamed
    /// </summary>
    [JsonPropertyName("oldUri")]
    [JsonRequired]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri OldUri { get; set; }

    /// <summary>
    /// A <c>file://</c> URI for the new location of the file/folder being renamed.
    /// </summary>
    [JsonPropertyName("newUri")]
    [JsonRequired]
    [JsonConverter(typeof(DocumentUriConverter))]
    public Uri NewUri { get; set; }
}
