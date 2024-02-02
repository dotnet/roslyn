// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class ObjectWriterExtensions
    {
        public static void WriteArray<T>(this ObjectWriter writer, ImmutableArray<T> values, Action<ObjectWriter, T> write)
        {
            writer.WriteInt32(values.Length);
            foreach (var val in values)
                write(writer, val);
        }

        public static async ValueTask WriteArrayAsync<T>(
            this ObjectWriter writer, ImmutableArray<T> values, Func<ObjectWriter, T, ValueTask> write)
        {
            writer.WriteInt32(values.Length);
            foreach (var val in values)
                await write(writer, val).ConfigureAwait(false);
        }
    }

    internal static class ObjectReaderExtensions
    {
        public static ValueTask<ImmutableArray<T>> ReadArrayAsync<T>(this ObjectReader reader, Func<ObjectReader, T> read)
            => ReadArrayAsync(reader, static (reader, read) => new ValueTask<T>(read(reader)), read);

        public static ValueTask<ImmutableArray<T>> ReadArrayAsync<T>(this ObjectReader reader, Func<ObjectReader, ValueTask<T>> read)
            => ReadArrayAsync(reader, static (reader, read) => read(reader), read);

        public static ValueTask<ImmutableArray<T>> ReadArrayAsync<T, TArg>(this ObjectReader reader, Func<ObjectReader, TArg, T> read, TArg arg)
            => ReadArrayAsync(reader, static (reader, tuple) => new ValueTask<T>(tuple.read(reader, tuple.arg)), (read, arg));

        public static async ValueTask<ImmutableArray<T>> ReadArrayAsync<T, TArg>(
            this ObjectReader reader, Func<ObjectReader, TArg, ValueTask<T>> read, TArg arg)
        {
            var length = await reader.ReadInt32Async().ConfigureAwait(false);
            using var _ = ArrayBuilder<T>.GetInstance(length, out var builder);

            for (var i = 0; i < length; i++)
                builder.Add(await read(reader, arg).ConfigureAwait(false));

            return builder.ToImmutableAndClear();
        }
    }
}
