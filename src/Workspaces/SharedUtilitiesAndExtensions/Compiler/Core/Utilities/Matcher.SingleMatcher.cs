// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities;

internal partial class Matcher<T>
{
    private class SingleMatcher(Func<T, bool> predicate, string description) : Matcher<T>
    {
        public override bool TryMatch(IList<T> sequence, ref int index)
        {
            if (index < sequence.Count && predicate(sequence[index]))
            {
                index++;
                return true;
            }

            return false;
        }

        public override string ToString()
            => description;
    }
}
