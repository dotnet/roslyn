// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal struct RemoteClientInitializationOptions
{
    [JsonPropertyName("returnCodeActionAndRenamePathsWithPrefixedSlash")]
    public required bool ReturnCodeActionAndRenamePathsWithPrefixedSlash { get; set; }

    [JsonPropertyName("supportsFileManipulation")]
    public required bool SupportsFileManipulation { get; set; }

    [JsonPropertyName("showAllCSharpCodeActions")]
    public required bool ShowAllCSharpCodeActions { get; set; }
}
