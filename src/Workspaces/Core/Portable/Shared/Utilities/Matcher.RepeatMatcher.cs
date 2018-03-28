// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class RepeatMatcher : Matcher<T>
        {
            private readonly Matcher<T> _matcher;

            public RepeatMatcher(Matcher<T> matcher)
            {
                _matcher = matcher;
            }

            public override bool TryMatch(IList<T> sequence, ref int index)
            {
                while (_matcher.TryMatch(sequence, ref index))
                {
                }

                return true;
            }

            public override string ToString()
            {
                return string.Format("({0}*)", _matcher);
            }
        }
    }
}
