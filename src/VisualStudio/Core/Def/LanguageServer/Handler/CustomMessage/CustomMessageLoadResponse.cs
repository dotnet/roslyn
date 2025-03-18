// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Response for the <see cref="CustomMessageLoadHandler"/> request.
/// </summary>
/// <param name="CustomMessageHandlerNames">Names of the loaded non-document-specific custom message handlers.</param>
/// <param name="CustomMessageDocumentHandlerNames">Names of the loaded document-specific custom message handlers.</param>
internal readonly record struct CustomMessageLoadResponse(
    [property: JsonPropertyName("customMessageHandlerNames")] string[] CustomMessageHandlerNames,
    [property: JsonPropertyName("customMessageDocumentHandlerNames")] string[] CustomMessageDocumentHandlerNames);
