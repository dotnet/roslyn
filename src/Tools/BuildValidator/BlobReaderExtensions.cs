// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection.Metadata;

namespace BuildValidator
{
    internal static class BlobReaderExtensions
    {
        public static void SkipNullTerminator(ref this BlobReader blobReader)
        {
            var b = blobReader.ReadByte();
            if (b != '\0')
            {
                throw new InvalidDataException($"Encountered unexpected byte \"{b}\" when expecting a null terminator");
            }
        }
    }
}
