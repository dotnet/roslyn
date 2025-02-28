// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Parameters for the <see cref="CustomMessageDocumentHandler"/> request.
/// </summary>
/// <param name="assemblyPath">Full path to the assembly that contains the message handler.</param>
/// <param name="typeFullName">Full name of the <see cref="Type"/> of the message handler.</param>
/// <param name="message">Json message to be passed to a custom message handler.</param>
/// <param name="textDocument">Text document the <paramref name="message"/> refers to.</param>
/// <param name="positions">List of <see cref="Position"/> objects the <paramref name="message"/> refers to.
/// All elemements in <paramref name="positions"/> refer to <paramref name="textDocument"/>.</param>
internal readonly struct CustomMessageDocumentParams(string assemblyPath, string typeFullName, JsonNode message, TextDocumentIdentifier textDocument, Position[] positions)
{
    /// <summary>
    /// Gets the full path to the assembly that contains the message handler.
    /// </summary>
    [JsonPropertyName("assemblyPath")]
    public string AssemblyPath { get; } = Requires.NotNull(assemblyPath);

    /// <summary>
    /// Gets the full name of the <see cref="Type"/> of the message handler.
    /// </summary>
    [JsonPropertyName("typeFullName")]
    public string TypeFullName { get; } = Requires.NotNull(typeFullName);

    /// <summary>
    /// Gets the json message to be passed to a custom message handler.
    /// </summary>
    [JsonPropertyName("message")]
    public JsonNode Message { get; } = Requires.NotNull(message);

    /// <summary>
    /// Gets the text document the <see cref="Message"/> relates to.
    /// </summary>
    [JsonPropertyName("textDocument")]
    public TextDocumentIdentifier TextDocument { get; } = Requires.NotNull(textDocument);

    /// <summary>
    /// Gets the list of <see cref="Position"/> objects the <see cref="Message"/> refers to.
    /// </summary>
    /// <remarks>
    /// All elemements in <see cref="Positions"/> refer to <see cref="TextDocument"/>.
    /// </remarks>
    [JsonPropertyName("positions")]
    public Position[] Positions { get; } = Requires.NotNull(positions);
}
