// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    /// <summary>
    /// A wrapper for a solution that can be used by Razor for OOP services that communicate via System.Text.Json
    /// </summary>
    internal readonly record struct JsonSerializableRazorPinnedSolutionInfoWrapper(
        [property: JsonPropertyName("data1")] long Data1,
        [property: JsonPropertyName("data2")] long Data2,
        [property: JsonIgnore] Solution? Solution)
    {
        public static implicit operator JsonSerializableRazorPinnedSolutionInfoWrapper(RazorPinnedSolutionInfoWrapper info)
        {
            return new JsonSerializableRazorPinnedSolutionInfoWrapper(info.UnderlyingObject.Data1, info.UnderlyingObject.Data2, info.Solution);
        }

        public static implicit operator RazorPinnedSolutionInfoWrapper(JsonSerializableRazorPinnedSolutionInfoWrapper serializableDocumentId)
        {
            return new RazorPinnedSolutionInfoWrapper(new Checksum(serializableDocumentId.Data1, serializableDocumentId.Data2), serializableDocumentId.Solution);
        }
    }
}
