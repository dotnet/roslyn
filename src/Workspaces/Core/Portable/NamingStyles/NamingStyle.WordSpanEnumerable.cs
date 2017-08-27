// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NamingStyles
{
    internal partial struct NamingStyle
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
                => new WordSpanEnumerator(_name, _nameSpan, _wordSeparator);
        }
    }
}
