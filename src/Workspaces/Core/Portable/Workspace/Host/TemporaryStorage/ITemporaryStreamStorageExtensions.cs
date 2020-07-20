// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.CodeAnalysis.Host
{
    internal static class ITemporaryStreamStorageExtensions
    {
        public static void WriteString(this ITemporaryStreamStorage storage, string value)
        {
            using var stream = SerializableBytes.CreateWritableStream();
            using var writer = new StreamWriter(stream);

            writer.Write(value);
            writer.Flush();
            stream.Position = 0;

            storage.WriteStream(stream);
        }

        public static string ReadString(this ITemporaryStreamStorage storage)
        {
            using var stream = storage.ReadStream();
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
