// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The options to register for file operations
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#fileOperationRegistrationOptions">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class FileOperationRegistrationOptions
{
    /// <summary>
    /// The actual filters.
    /// </summary>
    [JsonPropertyName("filters")]
    [JsonRequired]
    public FileOperationFilter[] Filters { get; init; }
}
