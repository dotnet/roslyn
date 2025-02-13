// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

internal class RazorProvideDynamicFileParams
{
    [JsonPropertyName("razorDocument")]
    public required TextDocumentIdentifier RazorDocument { get; set; }

    /// <summary>
    /// When true, the full text of the document will be sent over as a single
    /// edit instead of diff edits
    /// </summary>
    [JsonPropertyName("fullText")]
    public bool FullText { get; set; }
}
