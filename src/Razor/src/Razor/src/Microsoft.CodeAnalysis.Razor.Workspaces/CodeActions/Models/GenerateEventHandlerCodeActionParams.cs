// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class GenerateEventHandlerCodeActionParams
{
    [JsonPropertyName("methodName")]
    public required string MethodName { get; set; }

    [JsonPropertyName("eventParameterType")]
    public string? EventParameterType { get; set; }

    [JsonPropertyName("isAsync")]
    public required bool IsAsync { get; set; }
}
