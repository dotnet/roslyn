// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    /// <inheritdoc cref="VirtualCharSequence"/>
    internal readonly struct AspNetCoreVirtualCharSequence
    {
        private readonly VirtualCharSequence _virtualCharSequence;

        internal AspNetCoreVirtualCharSequence(VirtualCharSequence virtualCharSequence)
        {
            _virtualCharSequence = virtualCharSequence;
        }

        /// <inheritdoc cref="VirtualCharSequence.Empty"/>
        public static readonly AspNetCoreVirtualCharSequence Empty = new(VirtualCharSequence.Empty);

        /// <inheritdoc cref="VirtualCharSequence.Length"/>
        public int Length => _virtualCharSequence.Length;

        /// <inheritdoc cref="VirtualCharSequence.this"/>
        public AspNetCoreVirtualChar this[int index] => new(_virtualCharSequence[index]);

        /// <inheritdoc cref="VirtualCharSequence.GetSubSequence"/>
        public AspNetCoreVirtualCharSequence GetSubSequence(TextSpan span) => new(_virtualCharSequence.GetSubSequence(span));

        /// <inheritdoc cref="VirtualCharSequence.Find"/>
        public AspNetCoreVirtualChar? Find(int position) => (_virtualCharSequence.Find(position) is VirtualChar c) ? new(c) : null;

        /// <inheritdoc cref="VirtualCharSequence.CreateString"/>
        public string CreateString() => _virtualCharSequence.CreateString();

        /// <inheritdoc cref="VirtualCharSequence.FromBounds"/>
        public static AspNetCoreVirtualCharSequence FromBounds(
            AspNetCoreVirtualCharSequence chars1, AspNetCoreVirtualCharSequence chars2) =>
            new(VirtualCharSequence.FromBounds(chars1._virtualCharSequence, chars2._virtualCharSequence));
    }
}
