// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Extensions;

/// <summary>
/// Return type for the workspace/_vs_dispatchExtensionMessage and textDocument/_vs_dipatchExtensionMessage request.
/// </summary>
/// <param name="Response">Json response returned by the extension message handler. Can be <see langword="null"/> if the
/// extension was unloaded concurrently with the response being issued, or if the extension threw an exception while
/// processing.</param>
internal readonly record struct DispatchExtensionMessageResponse(
    [property: JsonPropertyName("response"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Response,
    [property: JsonPropertyName("extensionWasUnloaded"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool ExtensionWasUnloaded,
    [property: JsonPropertyName("extensionException"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] Exception? ExtensionException);
