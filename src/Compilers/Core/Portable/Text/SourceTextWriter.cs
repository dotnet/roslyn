// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using Roslyn.Utilities;

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

        protected void ValidateWriteArguments(char[] buffer, int index, int count)
        {
            RoslynDebug.AssertNotNull(buffer);
            RoslynDebug.Assert(index >= 0 && index < buffer.Length);
            RoslynDebug.Assert(count >= 0 && count <= buffer.Length - index);
        }
    }
}
