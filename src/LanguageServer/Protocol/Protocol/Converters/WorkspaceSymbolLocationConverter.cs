// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

internal sealed class WorkspaceSymbolLocationConverter : JsonConverter<SumType<Location, WorkspaceSymbolLocation>>
{
    public override SumType<Location, WorkspaceSymbolLocation> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var location = document.RootElement;
        return location.ValueKind == JsonValueKind.Object && !location.TryGetProperty("range", out _)
            ? new(location.Deserialize<WorkspaceSymbolLocation>(options)!)
            : location.Deserialize<SumType<Location, WorkspaceSymbolLocation>>(options);
    }

    public override void Write(Utf8JsonWriter writer, SumType<Location, WorkspaceSymbolLocation> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
