// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct HoistedLocalScopeRecord
    {
        public readonly int StartOffset;
        public readonly int Length;

        private HoistedLocalScopeRecord(int startOffset, int length)
        {
            this.StartOffset = startOffset;
            this.Length = length;
        }

        public static HoistedLocalScopeRecord FromNative(int startOffset, int endOffsetInclusive)
        {
            return new HoistedLocalScopeRecord(startOffset, endOffsetInclusive - startOffset + 1);
        }

        public static HoistedLocalScopeRecord FromPortable(int startOffset, int length)
        {
            return new HoistedLocalScopeRecord(startOffset, length);
        }
    }
}
