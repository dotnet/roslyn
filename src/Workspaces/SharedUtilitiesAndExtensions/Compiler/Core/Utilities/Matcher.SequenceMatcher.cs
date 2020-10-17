﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class SequenceMatcher : Matcher<T>
        {
            private readonly Matcher<T>[] _matchers;

            public SequenceMatcher(params Matcher<T>[] matchers)
                => _matchers = matchers;

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
                => string.Format("({0})", string.Join(",", (object[])_matchers));
        }
    }
}
