// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Remote.CustomMessageHandler;

/// <summary>
/// Reads a <see cref="LinePosition"/> object that were written as indexes of the provided <paramref name="linePositions"/> array.
/// </summary>
internal sealed class LinePositionReadConverter(ImmutableArray<LinePosition> linePositions) : JsonConverter<LinePosition>
{
    private readonly ImmutableArray<LinePosition> _linePositions = linePositions;

    public override LinePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException($"Expected a number, found {reader.TokenType}");
        }

        var index = reader.GetInt32();
        if (index < 0 || index >= this._linePositions.Length)
        {
            throw new JsonException($"Invalid LinePosition index {index}, a maximum of {this._linePositions.Length} positions are available for reading.");
        }

        return this._linePositions[index];
    }

    public override void Write(Utf8JsonWriter writer, LinePosition value, JsonSerializerOptions options)
    {
        throw new NotSupportedException($"Writing is not supported by {nameof(LinePositionReadConverter)}");
    }
}
