// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Remote;

/// <summary>
/// A wrapper for a solution that can be used by Razor for OOP services that communicate via System.Text.Json
/// </summary>
internal readonly record struct JsonSerializableRazorSolutionWrapper(
    [property: JsonPropertyName("data1")] long Data1,
    [property: JsonPropertyName("data2")] long Data2,
    [property: JsonIgnore] Solution? Solution)
{
    public static implicit operator JsonSerializableRazorSolutionWrapper(RazorSolutionWrapper info)
    {
        return new JsonSerializableRazorSolutionWrapper(info.Checksum.Data1, info.Checksum.Data2, info.Solution);
    }

    public static implicit operator RazorSolutionWrapper(JsonSerializableRazorSolutionWrapper serializableDocumentId)
    {
        return new RazorSolutionWrapper(new Checksum(serializableDocumentId.Data1, serializableDocumentId.Data2), serializableDocumentId.Solution);
    }
}
