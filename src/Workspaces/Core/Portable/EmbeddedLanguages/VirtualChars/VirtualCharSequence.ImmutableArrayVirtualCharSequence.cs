// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        private class ImmutableArrayVirtualCharSequence
            : AbstractVirtualCharSequence<ImmutableArray<VirtualChar>>
        {
            public ImmutableArrayVirtualCharSequence(
                ImmutableArray<VirtualChar> underlyingData, TextSpan underlyingDataSpan)
                : base(underlyingData, underlyingDataSpan, underlyingData.Length)
            {
            }

            public override VirtualChar this[int index]
                => UnderlyingData[UnderlyingDataSpan.Start + index];

            public override VirtualCharSequence GetSubSequence(TextSpan span)
                => Create(UnderlyingData, new TextSpan(UnderlyingDataSpan.Start + span.Start, span.Length));

            public override string CreateString()
                => UnderlyingData.CreateString(UnderlyingDataSpan);
        }
    }
}
