// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Return type for the roslyn/customMessage and roslyn/customDocumentMessage request.
/// </summary>
/// <param name="Response">Json response returned by the custom message handler.</param>
internal readonly record struct CustomResponse(
    [property: JsonPropertyName("response")] string Response);
