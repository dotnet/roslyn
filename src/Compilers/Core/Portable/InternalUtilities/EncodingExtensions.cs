// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Roslyn.Utilities
{
    internal static class EncodingExtensions
    {
        /// <summary>
        /// Get maximum char count needed to decode the entire stream.
        /// </summary>
        /// <exception cref="IOException">Stream is so big that max char count can't fit in <see cref="int"/>.</exception> 
        internal static int GetMaxCharCountOrThrowIfHuge(this Encoding encoding, Stream stream)
        {
            Debug.Assert(stream.CanSeek);
            long length = stream.Length;

            if (encoding.TryGetMaxCharCount(length, out int maxCharCount))
            {
                return maxCharCount;
            }

#if WORKSPACE
            throw new IOException(WorkspacesResources.Stream_is_too_long);
#else
            throw new IOException(CodeAnalysisResources.StreamIsTooLong);
#endif
        }

        internal static bool TryGetMaxCharCount(this Encoding encoding, long length, out int maxCharCount)
        {
            maxCharCount = 0;

            if (length <= int.MaxValue)
            {
                try
                {
                    maxCharCount = encoding.GetMaxCharCount((int)length);
                    return true;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Encoding does not provide a way to predict that max byte count would not
                    // fit in Int32 and we must therefore catch ArgumentOutOfRange to handle that
                    // case.
                }
            }

            return false;
        }
    }
}
