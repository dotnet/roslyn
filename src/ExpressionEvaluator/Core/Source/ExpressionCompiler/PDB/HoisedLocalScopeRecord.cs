// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal struct HoistedLocalScopeRecord
    {
        public readonly int StartOffset;
        public readonly int Length;

        public HoistedLocalScopeRecord(int startOffset, int length)
        {
            this.StartOffset = startOffset;
            this.Length = length;
        }
    }
}
