// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class ChoiceMatcher : Matcher<T>
        {
            private readonly IEnumerable<Matcher<T>> _matchers;

            public ChoiceMatcher(params Matcher<T>[] matchers)
            {
                _matchers = matchers;
            }

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
            {
                return $"({string.Join("|", _matchers)})";
            }
        }
    }
}
