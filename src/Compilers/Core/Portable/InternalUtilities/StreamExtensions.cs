// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class StreamExtensions
    {
        // From System.IO.Stream.CopyTo:
        // We pick a value that is the largest multiple of 4096 that is still smaller than the large object heap threshold (85K).
        // The CopyTo/CopyToAsync buffer is short-lived and is likely to be collected at Gen0, and it offers a significant
        // improvement in Copy performance.
        internal const int StreamCopyBufferSize = 81920;

        /// <summary>
        /// Copies specified amount of data from given stream to a target memory pointer.
        /// </summary>
        /// <exception cref="IOException">unexpected stream end.</exception>
        internal static void CopyTo(this Stream source, IntPtr destination, int size)
        {
            byte[] buffer = new byte[Math.Min(StreamCopyBufferSize, size)];
            while (size > 0)
            {
                int readSize = Math.Min(size, buffer.Length);
                int bytesRead = source.Read(buffer, 0, readSize);

                if (bytesRead <= 0 || bytesRead > readSize)
                {
                    throw new IOException(CodeAnalysisResources.UnexpectedStreamEnd);
                }

                Marshal.Copy(buffer, 0, destination, bytesRead);

                destination += bytesRead;
                size -= bytesRead;
            }
        }
    }
}
