// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        /// <summary>
        /// Implementation of <see cref="VirtualCharSequence"/> that represents the
        /// concatenation of two sequences.
        /// </summary>
        private class ConcatVirtualCharSequence : VirtualCharSequence
        {
            private readonly VirtualCharSequence _first;
            private readonly VirtualCharSequence _second;

            public ConcatVirtualCharSequence(VirtualCharSequence first, VirtualCharSequence second)
            {
                _first = first;
                _second = second;
                Length = first.Length + second.Length;
            }

            public override VirtualChar this[int index]
                => index < _first.Length ? _first[index] : _second[index - _first.Length];

            public override int Length { get; }

            protected override string CreateStringWorker()
                => _first.CreateString() + _second.CreateString();
        }
    }
}
