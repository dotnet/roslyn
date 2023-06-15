// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NamingStyles
{
    internal partial record struct NamingStyle
    {
        private readonly struct WordSpanEnumerable(string name, TextSpan nameSpan, string wordSeparator)
        {
            public WordSpanEnumerator GetEnumerator()
                => new(name, nameSpan, wordSeparator);
        }
    }
}
