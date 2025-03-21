// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

/// <summary>
/// Parameters for the roslyn/customMessage request.
/// </summary>
/// <param name="MessageName">Name of the custom message to be invoked.</param>
/// <param name="Message">Json message to be passed to a custom message handler.</param>
internal readonly record struct CustomMessageParams(
    [property: JsonPropertyName("messageName")] string MessageName,
    [property: JsonPropertyName("message")] string Message);
