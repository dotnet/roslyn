// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
        /// <summary>
        /// Represents a <see cref="VirtualCharSequence"/> efficiently on top of
        /// a single character.  Useful for that common case, as well as when getting
        /// a single char subsequence of another sequence.
        /// </summary>
        private class SingleVirtualCharSequence : VirtualCharSequence
        {
            private readonly VirtualChar _ch;

            public SingleVirtualCharSequence(VirtualChar ch)
            {
                _ch = ch;
            }

            public override VirtualChar this[int index]
            {
                get
                {
                    if (index != 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }

                    return _ch;
                }
            }

            public override int Length => 1;

            protected override string CreateStringWorker() => _ch.Char.ToString();
        }
    }
}
