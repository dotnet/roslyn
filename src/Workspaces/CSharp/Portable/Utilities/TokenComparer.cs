// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.CSharp.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    internal class TokenComparer : IComparer<SyntaxToken>
    {
        public static readonly IComparer<SyntaxToken> NormalInstance = new TokenComparer(specialCaseSystem: false);
        public static readonly IComparer<SyntaxToken> SystemFirstInstance = new TokenComparer(specialCaseSystem: true);

        private readonly bool _specialCaseSystem;

        private TokenComparer(bool specialCaseSystem)
        {
            _specialCaseSystem = specialCaseSystem;
        }

        public int Compare(SyntaxToken x, SyntaxToken y)
        {
            if (_specialCaseSystem &&
                x.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.UsingKeyword, SyntaxKind.StaticKeyword) &&
                y.GetPreviousToken(includeSkipped: true).IsKind(SyntaxKind.UsingKeyword, SyntaxKind.StaticKeyword))
            {
                var token1IsSystem = x.ValueText == nameof(System);
                var token2IsSystem = y.ValueText == nameof(System);

                if (token1IsSystem && !token2IsSystem)
                {
                    return -1;
                }
                else if (!token1IsSystem && token2IsSystem)
                {
                    return 1;
                }
            }

            return CompareWorker(x, y);
        }

        private int CompareWorker(SyntaxToken x, SyntaxToken y)
        {
            if (x == y)
            {
                return 0;
            }

            // By using 'ValueText' we get the value that is normalized.  i.e.
            // @class will be 'class', and Unicode escapes will be converted
            // to actual Unicode.  This allows sorting to work properly across
            // tokens that have different source representations, but which
            // mean the same thing.
            var string1 = x.ValueText;
            var string2 = y.ValueText;

            // First check in a case insensitive manner.  This will put 
            // everything that starts with an 'a' or 'A' above everything
            // that starts with a 'b' or 'B'.
            var compare = CultureInfo.InvariantCulture.CompareInfo.Compare(string1, string2,
                CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth);
            if (compare != 0)
            {
                return compare;
            }

            // Now, once we've grouped such that 'a' words and 'A' words are
            // together, sort such that 'a' words come before 'A' words.
            return CultureInfo.InvariantCulture.CompareInfo.Compare(string1, string2,
                CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth);
        }
    }
}
