// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

internal class LinePositionReadConverter : JsonConverter<LinePosition>
{
    private readonly IReadOnlyList<LinePosition> linePositions;

    public LinePositionReadConverter(IReadOnlyList<LinePosition> linePositions)
    {
        this.linePositions = linePositions;
    }

    public override LinePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException();
        }

        return this.linePositions[reader.GetInt32()];
    }

    public override void Write(Utf8JsonWriter writer, LinePosition value, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}
