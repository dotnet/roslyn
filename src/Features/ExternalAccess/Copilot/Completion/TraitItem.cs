// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Completion;

internal record TraitItem : IContextItem
{
    public TraitItem(string name, string value, int importance = Completion.Importance.Default)
    {
        this.Name = name;
        this.Value = value;
        this.Importance = importance;
    }

    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("value")]
    public string Value { get; init; }

    [JsonPropertyName("importance")]
    public int Importance { get; init; }
}
