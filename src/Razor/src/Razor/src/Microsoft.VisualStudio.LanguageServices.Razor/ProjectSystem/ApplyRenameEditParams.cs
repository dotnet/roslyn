// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.VisualStudio.Razor.ProjectSystem;

internal class ApplyRenameEditParams
{
    [JsonPropertyName("edit")]
    public required WorkspaceEdit Edit { get; set; }

    [JsonPropertyName("oldFilePath")]
    public required string OldFilePath { get; set; }

    [JsonPropertyName("newFilePath")]
    public required string NewFilePath { get; set; }
}
