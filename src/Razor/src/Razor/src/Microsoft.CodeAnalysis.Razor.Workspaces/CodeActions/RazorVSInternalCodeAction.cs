// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.CodeActions.Models;

[DataContract]
internal sealed class RazorVSInternalCodeAction : VSInternalCodeAction
{
    [JsonPropertyName("name")]
    [DataMember(Name = "name")]
    public string? Name { get; set; }

    /// <summary>
    /// The order code actions should appear. This is not serialized as its just used in the code actions service
    /// </summary>
    [JsonIgnore]
    public int Order { get; set; }
}
