// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class ChoiceMatcher(params Matcher<T>[] matchers) : Matcher<T>
        {
            private readonly IEnumerable<Matcher<T>> _matchers = matchers;

            public override bool TryMatch(IList<T> sequence, ref int index)
            {
                // we can't use .Any() here because ref parameters can't be used in lambdas
                foreach (var matcher in _matchers)
                {
                    if (matcher.TryMatch(sequence, ref index))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override string ToString()
                => $"({string.Join("|", _matchers)})";
        }
    }
}
