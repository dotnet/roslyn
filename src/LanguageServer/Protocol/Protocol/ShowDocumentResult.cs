// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// The result of a 'windows/showDocument' request.
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#showDocumentResult">Language Server Protocol specification</see> for additional information.
/// </para>
/// </summary>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class ShowDocumentResult
{
    /// <summary>
    /// Indicates whether the show was successful.
    /// </summary>
    [JsonPropertyName("success")]
    [JsonRequired]
    public bool Success { get; init; }
}
