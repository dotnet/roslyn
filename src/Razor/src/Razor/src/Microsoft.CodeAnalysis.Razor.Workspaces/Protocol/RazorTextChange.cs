// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Protocol;

/// <summary>
/// A representation of a Roslyn TextChange that can be serialized with System.Text.Json. Also needs to match
/// https://github.com/dotnet/vscode-csharp/blob/main/src/razor/src/rpc/serverTextChange.ts for VS Code.
/// </summary>
internal sealed record RazorTextChange
{
    [JsonPropertyName("span")]
    public required RazorTextSpan Span { get; set; }

    [JsonPropertyName("newText")]
    public string? NewText { get; set; }
}
