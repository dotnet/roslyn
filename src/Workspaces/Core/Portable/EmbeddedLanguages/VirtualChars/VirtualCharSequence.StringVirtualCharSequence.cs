// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        private class StringVirtualCharSequence : AbstractVirtualCharSequence<string>
        {
            private readonly int _position;

            public StringVirtualCharSequence(
                string underlyingData, int position, TextSpan underlyingDataSpan)
                : base(underlyingData, underlyingDataSpan, underlyingData.Length)
            {
                _position = position;
            }

            public override VirtualChar this[int index]
                => new VirtualChar(
                    UnderlyingData[UnderlyingDataSpan.Start + index],
                    new TextSpan(_position + index, length: 1));

            public override VirtualCharSequence GetSubSequence(TextSpan span)
                => Create(
                    UnderlyingData,
                    _position + span.Start,
                    new TextSpan(UnderlyingDataSpan.Start + span.Start, span.Length));

            public override string CreateString()
                => UnderlyingData.Substring(UnderlyingDataSpan.Start, UnderlyingDataSpan.Length);
        }
    }
}
