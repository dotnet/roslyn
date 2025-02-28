// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Return type for the <see cref="CustomMessageDocumentHandler"/> request.
/// </summary>
/// <param name="response">Json response returned by the custom message handler.</param>
/// <param name="positions">List of <see cref="Position"/> objects the <paramref name="response"/> refers to.
/// All elemements in <paramref name="positions"/> refer to <see cref="CustomMessageDocumentParams.TextDocument"/>.</param>
internal readonly struct CustomDocumentResponse(JsonNode response, Position[] positions)
{
    /// <summary>
    /// Gets the json response returned by the custom message handler.
    /// </summary>
    [JsonPropertyName("response")]
    public JsonNode Response { get; } = Requires.NotNull(response);

    /// <summary>
    /// Gets the list of <see cref="Position"/> objects the <see cref="Response"/> refers to.
    /// </summary>
    /// <remarks>
    /// All elemements in <see cref="Positions"/> refer to <see cref="CustomMessageDocumentParams.TextDocument"/>.
    /// </remarks>
    [JsonPropertyName("positions")]
    public Position[] Positions { get; } = Requires.NotNull(positions);
}
