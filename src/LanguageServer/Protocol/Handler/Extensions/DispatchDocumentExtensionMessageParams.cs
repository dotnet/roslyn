// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

/// <summary>
/// Parameters for the textDocument/_vs_dipatchExtensionMessage request.
/// </summary>
/// <param name="MessageName">Name of the extension message to be invoked.</param>
/// <param name="Message">Json message to be passed to an extension message handler.</param>
/// <param name="TextDocument">Text document the <paramref name="Message"/> refers to.</param>
internal readonly record struct DispatchDocumentExtensionMessageParams(
    [property: JsonPropertyName("messageName")] string MessageName,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("textDocument")] TextDocumentIdentifier TextDocument);
