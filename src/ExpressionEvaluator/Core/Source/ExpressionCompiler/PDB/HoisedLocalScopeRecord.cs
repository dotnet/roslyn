// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal readonly struct HoistedLocalScopeRecord
    {
        public readonly int StartOffset;
        public readonly int Length;

        public HoistedLocalScopeRecord(int startOffset, int length)
        {
            StartOffset = startOffset;
            Length = length;
        }
    }
}
