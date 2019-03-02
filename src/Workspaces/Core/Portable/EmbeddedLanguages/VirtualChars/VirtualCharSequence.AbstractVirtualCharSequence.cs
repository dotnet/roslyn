// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        private abstract class AbstractVirtualCharSequence<TData> : VirtualCharSequence
        {
            protected readonly TData UnderlyingData;
            protected readonly TextSpan UnderlyingDataSpan;

            public AbstractVirtualCharSequence(
                TData data, TextSpan span, int underlyingDataLength)
            {
                if (span.Start > underlyingDataLength)
                {
                    throw new ArgumentException();
                }

                if (span.End > underlyingDataLength)
                {
                    throw new ArgumentException();
                }

                UnderlyingData = data;
                UnderlyingDataSpan = span;
            }

            public sealed override int Length => UnderlyingDataSpan.Length;
        }
    }
}
