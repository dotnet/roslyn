// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages
{
    internal readonly struct AspNetCoreVirtualCharSequence
    {
        private readonly VirtualCharSequence _virtualCharSequence;

        internal AspNetCoreVirtualCharSequence(VirtualCharSequence virtualCharSequence)
        {
            _virtualCharSequence = virtualCharSequence;
        }

        public int Length => _virtualCharSequence.Length;
        public AspNetCoreVirtualChar this[int index] => new(_virtualCharSequence[index]);
    }
}
