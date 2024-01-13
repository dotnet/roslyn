// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class ITemporaryStreamStorageExtensions
    {
        public static void WriteAllLines(this ITemporaryStreamStorageInternal storage, ImmutableArray<string> values)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new StreamWriter(stream);

            foreach (var value in values)
            {
                writer.WriteLine(value);
            }

            writer.Flush();
            stream.Position = 0;

            storage.WriteStream(stream);
        }

        public static ImmutableArray<string> ReadLines(this ITemporaryStreamStorageInternal storage)
        {
            return EnumerateLines(storage).ToImmutableArray();
        }

        private static IEnumerable<string> EnumerateLines(ITemporaryStreamStorageInternal storage)
        {
            using var stream = storage.ReadStream();
            using var reader = new StreamReader(stream);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }
    }
}
