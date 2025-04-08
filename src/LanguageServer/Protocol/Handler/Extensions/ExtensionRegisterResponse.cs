// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

internal sealed record class ExtensionException(
    [property: JsonPropertyName("typeName")] string TypeName,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("stackTrace"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? StackTrace,
    [property: JsonPropertyName("innerException"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] ExtensionException? InnerException)
{
    public static ExtensionException? FromException(Exception? exception)
        => exception is null
            ? null
            : new ExtensionException(exception.GetType().ToString(), exception.Message, exception.StackTrace, FromException(exception.InnerException));
}

/// <summary>
/// Response for the roslyn/extensionRegister request.
/// </summary>
/// <param name="WorkspaceMessageHandlers">Names of the registered non-document-specific extension message handlers.</param>
/// <param name="DocumentMessageHandlers">Names of the registered document-specific extension message handlers.</param>
/// <param name="ExtensionException">Details of any exceptions that occurred during extension registration.</param>
internal sealed record class ExtensionRegisterResponse(
    [property: JsonPropertyName("workspaceMessageHandlers")] ImmutableArray<string> WorkspaceMessageHandlers,
    [property: JsonPropertyName("documentMessageHandlers")] ImmutableArray<string> DocumentMessageHandlers,
    [property: JsonPropertyName("extensionException"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        ExtensionException? ExtensionException);
