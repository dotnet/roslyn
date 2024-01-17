// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    internal abstract class SourceTextWriter : TextWriter
    {
        /// <summary>
        /// Creates a <see cref="SourceText"/> from the written text. The length of the written text must be
        /// equal to the length provided to <see cref="Create(Encoding?, SourceHashAlgorithm, int)"/>.
        /// </summary>
        public abstract SourceText ToSourceText();

        /// <summary>
        /// Creates a <see cref="SourceTextWriter"/>. The provided length must be equal to the total length
        /// that will be written to it.
        /// </summary>
        public static SourceTextWriter Create(Encoding? encoding, SourceHashAlgorithm checksumAlgorithm, int length)
        {
            if (length < SourceText.LargeObjectHeapLimitInChars)
            {
                return new StringTextWriter(encoding, checksumAlgorithm, length);
            }
            else
            {
                return new LargeTextWriter(encoding, checksumAlgorithm, length);
            }
        }
    }
}
