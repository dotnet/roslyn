// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        private class ConcatVirtualCharSequence : VirtualCharSequence
        {
            private readonly VirtualCharSequence _first;
            private readonly VirtualCharSequence _second;
            private string _string;

            public ConcatVirtualCharSequence(VirtualCharSequence first, VirtualCharSequence second)
            {
                _first = first;
                _second = second;
                Length = first.Length + second.Length;
            }

            public override VirtualChar this[int index]
                => index < _first.Length ? _first[index] : _second[index - _first.Length];

            public override int Length { get; }

            public override string CreateString()
            {
                if (_string == null)
                {
                    _string = _first.CreateString() + _second.CreateString();
                }

                return _string;
            }

            public override VirtualCharSequence GetSubSequence(TextSpan span)
            {
                var temp = ArrayBuilder<VirtualChar>.GetInstance();
                for (var i = span.Start; i < span.End; i++)
                {
                    temp.Add(this[i]);
                }

                return Create(temp.ToImmutableAndFree());
            }
        }
    }
}
