// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.RegularExpressions
{
    internal static class RegexHelpers
    {
        public static TextSpan GetSpan(RegexToken token)
            => GetSpan(token.VirtualChars);

        public static TextSpan GetSpan(RegexToken token1, RegexToken token2)
            => GetSpan(token1.VirtualChars[0], token2.VirtualChars.Last());

        public static TextSpan GetSpan(ImmutableArray<VirtualChar> virtualChars)
            => GetSpan(virtualChars[0], virtualChars.Last());

        public static TextSpan GetSpan(VirtualChar firstChar, VirtualChar lastChar)
            => TextSpan.FromBounds(firstChar.Span.Start, lastChar.Span.End);

        public static bool HasOption(RegexOptions options, RegexOptions val)
            => (options & val) != 0;

        public static RegexOptions OptionFromCode(VirtualChar ch)
        {
            switch (ch)
            {
                case 'i': case 'I':
                    return RegexOptions.IgnoreCase;
                case 'm': case 'M':
                    return RegexOptions.Multiline;
                case 'n': case 'N':
                    return RegexOptions.ExplicitCapture;
                case 's': case 'S':
                    return RegexOptions.Singleline;
                case 'x': case 'X':
                    return RegexOptions.IgnorePatternWhitespace;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
