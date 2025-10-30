// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.NamingStyles;

internal partial record struct NamingStyle
{
    private struct WordSpanEnumerator
    {
        private readonly string _name;
        private readonly TextSpan _nameSpan;
        private readonly string _wordSeparator;

        public WordSpanEnumerator(string name, TextSpan nameSpan, string wordSeparator)
        {
            Debug.Assert(nameSpan.Length > 0);
            _name = name;
            _nameSpan = nameSpan;
            _wordSeparator = wordSeparator;
            Current = new TextSpan(nameSpan.Start, 0);
        }

        public TextSpan Current { get; private set; }

        public bool MoveNext()
        {
            if (_wordSeparator == "")
            {
                // No separator.  So only ever return a single word
                if (Current.Length == 0)
                {
                    Current = _nameSpan;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            while (true)
            {
                var nextWordSeparator = _name.IndexOf(_wordSeparator, Current.End);
                if (nextWordSeparator == Current.End)
                {
                    // We're right at the word separator.  Skip it and continue searching.
                    Current = new TextSpan(Current.End + _wordSeparator.Length, 0);
                    continue;
                }

                // If didn't find a word separator, it's as if the next word separator is at the end of name span.
                if (nextWordSeparator < 0)
                {
                    nextWordSeparator = _nameSpan.End;
                }

                // If we've walked past the _nameSpan just immediately stop.  There are no more words to return.
                if (Current.End > _nameSpan.End)
                {
                    return false;
                }

                // found a separator in front of us.  Note: it may be in our suffix portion.  
                // So use the min of the separator position and our end position.
                Current = TextSpan.FromBounds(Current.End, Math.Min(_nameSpan.End, nextWordSeparator));
                break;
            }

            return Current.Length > 0 && Current.End <= _nameSpan.End;
        }
    }
}
