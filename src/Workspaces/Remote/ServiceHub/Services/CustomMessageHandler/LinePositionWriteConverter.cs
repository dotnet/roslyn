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
/// Writes a <see cref="LinePosition"/> objects as incremental, 0-based indexes.
/// Distinct <see cref="LinePosition"/> objects and their associated index are accumulated
/// in <see cref="LinePositions"/> and are accessible when the write operation is complete.
/// </summary>
internal sealed class LinePositionWriteConverter : JsonConverter<LinePosition>
{
    /// <summary>
    /// A dictionary of <see cref="LinePosition"/> objects and their associated indexes that
    /// were used during the write operations.
    /// </summary>
    public ImmutableDictionary<LinePosition, int> LinePositions { get; private set; } = ImmutableDictionary.Create<LinePosition, int>();

    public override LinePosition Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotSupportedException($"Reading is not supported by {nameof(LinePositionWriteConverter)}");
    }

    public override void Write(Utf8JsonWriter writer, LinePosition value, JsonSerializerOptions options)
    {
        if (!this.LinePositions.TryGetValue(value, out var index))
        {
            index = this.LinePositions.Count;
            this.LinePositions = this.LinePositions.Add(value, index);
        }

        writer.WriteNumberValue(index);
    }
}
