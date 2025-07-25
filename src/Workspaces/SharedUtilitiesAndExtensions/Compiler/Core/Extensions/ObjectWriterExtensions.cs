// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static class ObjectWriterExtensions
{
    extension(ObjectWriter writer)
    {
        public void WriteArray<T>(ImmutableArray<T> values, Action<ObjectWriter, T> write)
        {
            writer.WriteInt32(values.Length);
            foreach (var val in values)
                write(writer, val);
        }
    }
}

internal static class ObjectReaderExtensions
{
    extension(ObjectReader reader)
    {
        public ImmutableArray<T> ReadArray<T>(Func<ObjectReader, T> read)
        => ReadArray(reader, static (reader, read) => read(reader), read);

        public ImmutableArray<T> ReadArray<T, TArg>(Func<ObjectReader, TArg, T> read, TArg arg)
        {
            var length = reader.ReadInt32();
            var builder = new FixedSizeArrayBuilder<T>(length);

            for (var i = 0; i < length; i++)
                builder.Add(read(reader, arg));

            return builder.MoveToImmutable();
        }
    }
}
