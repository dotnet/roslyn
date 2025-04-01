// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Roslyn.LanguageServer.Protocol;

/// <summary>
/// JsonConverter to correctly deserialize int arrays in the Label param of ParameterInformation.
/// </summary>
internal sealed class ParameterInformationConverter : JsonConverter<ParameterInformation>
{
    public override ParameterInformation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        var parameter = new ParameterInformation();

        if (root.TryGetProperty("label", out var labelElement))
        {
            if (labelElement.ValueKind == JsonValueKind.Array)
            {
                parameter.Label = new Tuple<int, int>(labelElement[0].GetInt32(), labelElement[1].GetInt32());
            }
            else
            {
                parameter.Label = labelElement.Deserialize<SumType<string, Tuple<int, int>>>(options);
            }
        }

        if (root.TryGetProperty("documentation", out var documentationElement))
        {
            parameter.Documentation = documentationElement.Deserialize<SumType<string, MarkupContent>>(options);
        }

        return parameter;
    }

    public override void Write(Utf8JsonWriter writer, ParameterInformation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("label");
        JsonSerializer.Serialize(writer, value.Label, options);

        if (value.Documentation != null)
        {
            writer.WritePropertyName("documentation");
            JsonSerializer.Serialize(writer, value.Documentation, options);
        }

        writer.WriteEndObject();
    }
}
