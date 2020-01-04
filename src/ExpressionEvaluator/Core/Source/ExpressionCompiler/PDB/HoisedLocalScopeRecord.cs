// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

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
