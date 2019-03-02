// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        private class SubSequenceVirtualCharSequence : VirtualCharSequence
        {
            private readonly VirtualCharSequence _sequence;
            private readonly TextSpan _span;

            public SubSequenceVirtualCharSequence(VirtualCharSequence sequence, TextSpan span)
            {
                if (span.Start > sequence.Length)
                {
                    throw new ArgumentException();
                }

                if (span.End > sequence.Length)
                {
                    throw new ArgumentException();
                }

                _sequence = sequence;
                _span = span;
            }

            public override int Length => _span.Length;

            public override VirtualChar this[int index] => _sequence[_span.Start + index];

            protected override string CreateStringWorker()
                => _sequence.CreateString().Substring(_span.Start, _span.Length);
        }
    }
}
