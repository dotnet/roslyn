// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

internal class CustomMessage(JsonNode message, TextDocumentIdentifier textDocument, Position[] positions)
{
    [JsonPropertyName("message")]
    public JsonNode Message { get; set; } = Requires.NotNull(message);

    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; set; } = Requires.NotNull(textDocument);

    [JsonPropertyName("positions")]
    public Position[] Positions { get; set; } = Requires.NotNull(positions);
}
