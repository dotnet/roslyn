// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;

namespace Microsoft.CodeAnalysis.Text
{
    internal abstract class SourceTextWriter : TextWriter
    {
        public abstract SourceText ToSourceText();

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
