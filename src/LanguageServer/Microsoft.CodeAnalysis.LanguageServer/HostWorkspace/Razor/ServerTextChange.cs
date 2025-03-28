// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.Razor;

internal class ServerTextChange
{
    [JsonPropertyName("span")]
    public required ServerTextSpan Span { get; set; }

    [JsonPropertyName("newText")]
    public required string NewText { get; set; }
}
