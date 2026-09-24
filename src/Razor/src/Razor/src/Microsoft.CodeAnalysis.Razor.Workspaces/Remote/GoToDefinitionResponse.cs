// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.CodeAnalysis.Razor.Remote;

internal sealed record GoToDefinitionResponse(
    [property: JsonPropertyName("locations")] LspLocation[]? Locations,
    [property: JsonPropertyName("csharpRequest")] TextDocumentPositionParams? CSharpRequest)
{
    public static GoToDefinitionResponse FromLocations(LspLocation[] locations)
        => new(locations, CSharpRequest: null);

    public static GoToDefinitionResponse FromCSharpRequest(TextDocumentPositionParams request)
        => new(Locations: null, request);
}
