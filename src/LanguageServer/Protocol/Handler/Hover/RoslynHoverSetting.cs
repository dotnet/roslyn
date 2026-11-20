// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

/// <summary>
/// Derived version of <see cref="HoverSetting" /> used so clients can opt into Roslyn-specific hover behavior
/// that is not part of the LSP specification.
/// </summary>
internal sealed class RoslynHoverSetting : HoverSetting
{
    /// <summary>
    /// Whether the client recognizes a leading U+200B (zero-width space) at the start of a C# declaration code block in
    /// hover markdown, and uses it to apply specialized syntax highlighting.
    /// </summary>
    [JsonPropertyName("_roslyn_supportsCSharpDeclarationMarkers")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SupportsCSharpDeclarationMarkers { get; set; }
}
