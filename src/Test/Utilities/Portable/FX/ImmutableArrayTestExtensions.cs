﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    /// <summary>
    /// The collection of extension methods for the <see cref="ImmutableArray{T}"/> type
    /// </summary>
    public static class ImmutableArrayTestExtensions
    {
        /// <summary>
        /// Writes read-only array of bytes to the specified file.
        /// </summary>
        /// <param name="bytes">Data to write to the file.</param>
        /// <param name="path">File path.</param>
        internal static void WriteToFile(this ImmutableArray<byte> bytes, string path)
        {
            Debug.Assert(!bytes.IsDefault);

            const int bufferSize = 4096;
            using (FileStream fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize))
            {
                // PERF: Consider using an ObjectPool<byte[]> here
                byte[] buffer = new byte[Math.Min(bufferSize, bytes.Length)];

                int offset = 0;
                while (offset < bytes.Length)
                {
                    int length = Math.Min(bufferSize, bytes.Length - offset);
                    bytes.CopyTo(offset, buffer, 0, length);
                    fileStream.Write(buffer, 0, length);
                    offset += length;
                }
            }
        }
    }
}
