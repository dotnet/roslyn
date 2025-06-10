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
        /// Generates a <see cref="SourceText"/> from previously written contents. After this method is called,
        /// the writer may be considered empty. Further calls to this writer will not reflect contents previously added.
        /// </summary>
        public abstract SourceText ToSourceTextAndFree();

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
