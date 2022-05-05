// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class AbstractVirtualCharService
    {
        private struct StringTextInfo : ITextInfo<string>
        {
            public char Get(string text, int index) => text[index];
            public int Length(string text) => text.Length;
        }
    }
}
