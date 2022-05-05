// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars
{
    internal abstract partial class AbstractVirtualCharService
    {
        private interface ITextInfo<T>
        {
            char Get(T text, int index);
            int Length(T text);
        }
    }
}
