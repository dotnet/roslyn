// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class SingleMatcher : Matcher<T>
        {
            private readonly Func<T, bool> _predicate;
            private readonly string _description;

            public SingleMatcher(Func<T, bool> predicate, string description)
            {
                _predicate = predicate;
                _description = description;
            }

            public override bool TryMatch(IList<T> sequence, ref int index)
            {
                if (index < sequence.Count && _predicate(sequence[index]))
                {
                    index++;
                    return true;
                }

                return false;
            }

            public override string ToString()
                => _description;
        }
    }
}
