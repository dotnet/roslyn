// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// Client capabilities for the 'window/showDocument' request
/// </summary>
/// <para>
/// See the <see href="https://microsoft.github.io/language-server-protocol/specifications/specification-current/#window_showDocument">Language Server Protocol specification</see> for additional information.
/// </para>
/// <remarks>Since LSP 3.16</remarks>
internal sealed class ShowDocumentClientCapabilities
{
    /// <summary>
    /// The client has support for the show document request.
    /// </summary>
    [JsonPropertyName("support")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Support { get; init; }
}
