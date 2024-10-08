// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal abstract partial class Matcher<T>
{
    private sealed class SequenceMatcher(params Matcher<T>[] matchers) : Matcher<T>
    {
        public override bool TryMatch(IList<T> sequence, ref int index)
        {
            var currentIndex = index;
            foreach (var matcher in matchers)
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
            => string.Format("({0})", string.Join(",", (object[])matchers));
    }
}
