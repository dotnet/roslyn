// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    /// <summary>
    /// Root of all <see cref="VirtualCharSequence"/>s that are produced directly
    /// by an ImmutableArray or string.  This type is technically not necessary.
    /// However, by having a strong type here, it helps ensure invariants. Specifically,
    /// that tokens don't point at this, and instead point <see cref="SubSequenceVirtualCharSequence"/>.
    /// </summary>
    internal abstract partial class LeafVirtualCharSequence : VirtualCharSequence
    {
        public SubSequenceVirtualCharSequence GetSubSequence(TextSpan span)
           => new SubSequenceVirtualCharSequence(this, span);
    }
}
