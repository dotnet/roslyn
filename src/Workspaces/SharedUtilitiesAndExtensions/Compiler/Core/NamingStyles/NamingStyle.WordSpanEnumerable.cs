// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NamingStyles
{
    internal partial record struct NamingStyle
    {
        private struct WordSpanEnumerable
        {
            private readonly string _name;
            private readonly TextSpan _nameSpan;
            private readonly string _wordSeparator;

            public WordSpanEnumerable(string name, TextSpan nameSpan, string wordSeparator)
            {
                _name = name;
                _nameSpan = nameSpan;
                _wordSeparator = wordSeparator;
            }

            public WordSpanEnumerator GetEnumerator()
                => new(_name, _nameSpan, _wordSeparator);
        }
    }
}
