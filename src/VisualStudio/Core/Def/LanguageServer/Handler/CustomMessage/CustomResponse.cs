// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Return type for the <see cref="CustomMessageHandler"/> request.
/// </summary>
/// <param name="response">Json response returned by the custom message handler.</param>
internal readonly struct CustomResponse(JsonNode response)
{
    /// <summary>
    /// Gets the json response returned by the custom message handler.
    /// </summary>
    [JsonPropertyName("response")]
    public JsonNode Response { get; } = Requires.NotNull(response);
}
