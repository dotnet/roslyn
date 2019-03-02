// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class VirtualCharSequence
    {
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

            public override string CreateString() => _ch.Char.ToString();
        }
    }
}
