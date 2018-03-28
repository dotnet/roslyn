// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class SequenceMatcher : Matcher<T>
        {
            private readonly Matcher<T>[] _matchers;

            public SequenceMatcher(params Matcher<T>[] matchers)
            {
                _matchers = matchers;
            }

            public override bool TryMatch(IList<T> sequence, ref int index)
            {
                var currentIndex = index;
                foreach (var matcher in _matchers)
                {
                    if (!matcher.TryMatch(sequence, ref currentIndex))
                    {
                        return false;
                    }
                }

                index = currentIndex;
                return true;
            }

            public override string ToString()
            {
                return string.Format("({0})", string.Join(",", (object[])_matchers));
            }
        }
    }
}
