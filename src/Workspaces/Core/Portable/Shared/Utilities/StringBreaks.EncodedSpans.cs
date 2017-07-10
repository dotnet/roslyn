// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial struct StringBreaks
    {
        private struct EncodedSpans
        {
            private const uint Mask = (1u << BitsPerEncodedSpan) - 1u;
            private uint _value;

            public byte this[int index]
            {
                get
                {
                    Debug.Assert(index >= 0 && index < MaxShortSpans);
                    return (byte)((_value >> (index * BitsPerEncodedSpan)) & Mask);
                }
                set
                {
                    Debug.Assert(index >= 0 && index < MaxShortSpans);
                    int shift = index * BitsPerEncodedSpan;
                    _value = (_value & ~(Mask << shift)) | ((uint)value << shift);
                }
            }
        }
    }
}
