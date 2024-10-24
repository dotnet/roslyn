// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.CustomMessage;

internal class LinePositionWriteConverter : JsonConverter<LinePosition>
{
    public Dictionary<LinePosition, int> LinePositions { get; } = new();

    public override LinePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }

    public override void Write(Utf8JsonWriter writer, LinePosition value, JsonSerializerOptions options)
    {
        if (!this.LinePositions.TryGetValue(value, out var index))
        {
            index = this.LinePositions.Count;
            this.LinePositions.Add(value, index);
        }

        writer.WriteNumberValue(index);
    }
}
