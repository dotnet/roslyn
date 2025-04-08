// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

/// <summary>
/// Return type for the roslyn/extensionWorkspaceMessage and roslyn/extensionDocumentMessage request.
/// </summary>
/// <param name="Response">Json response returned by the extension message handler.</param>
internal readonly record struct ExtensionMessageResponse(
    [property: JsonPropertyName("response")] string Response);
