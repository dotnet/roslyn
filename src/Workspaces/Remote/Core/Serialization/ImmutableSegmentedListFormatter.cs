// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MessagePack;
using MessagePack.Formatters;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class ImmutableSegmentedListFormatter<T> : IMessagePackFormatter<ImmutableSegmentedList<T>>
{
    ImmutableSegmentedList<T> IMessagePackFormatter<ImmutableSegmentedList<T>>.Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
            return default;

        var len = reader.ReadArrayHeader();
        if (len == 0)
            return ImmutableSegmentedList<T>.Empty;

        var formatter = options.Resolver.GetFormatterWithVerify<T>();

        // TODO: Should there be a CreateBuilder that takes in an initial capacity
        // similar to what ImmutableArray allows?
        var builder = ImmutableSegmentedList.CreateBuilder<T>();
        options.Security.DepthStep(ref reader);
        try
        {
            for (var i = 0; i < len; i++)
                builder.Add(formatter.Deserialize(ref reader, options));
        }
        finally
        {
            reader.Depth--;
        }

        return builder.ToImmutable();
    }

    void IMessagePackFormatter<ImmutableSegmentedList<T>>.Serialize(ref MessagePackWriter writer, ImmutableSegmentedList<T> value, MessagePackSerializerOptions options)
    {
        if (value.IsDefault)
        {
            writer.WriteNil();
        }
        else if (value.IsEmpty)
        {
            writer.WriteArrayHeader(0);
        }
        else
        {
            var formatter = options.Resolver.GetFormatterWithVerify<T>();

            writer.WriteArrayHeader(value.Count);
            foreach (var item in value)
                formatter.Serialize(ref writer, item, options);
        }
    }
}
