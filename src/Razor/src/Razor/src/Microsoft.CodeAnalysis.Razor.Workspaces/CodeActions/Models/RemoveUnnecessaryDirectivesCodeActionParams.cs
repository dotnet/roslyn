// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

internal sealed class RemoveUnnecessaryDirectivesCodeActionParams
{
    [JsonPropertyName("unusedDirectiveSpans")]
    public required RazorTextSpan[] UnusedDirectiveSpans { get; init; }
}
