// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;

internal record CodeSnippetItem : IContextItem
{
    public CodeSnippetItem(string uri, string value, string[]? additionalUris = null, int importance = Completion.Importance.Default)
    {
        this.Uri = uri;
        this.Value = value;
        this.AdditionalUris = additionalUris;
        this.Importance = importance;
    }

    [JsonPropertyName("uri")]
    public string Uri { get; init; }

    [JsonPropertyName("value")]
    public string Value { get; init; }

    [JsonPropertyName("additionalUris")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? AdditionalUris { get; init; }

    [JsonPropertyName("importance")]
    public int Importance { get; init; }
}
