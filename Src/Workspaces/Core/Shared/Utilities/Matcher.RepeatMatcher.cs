// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class RepeatMatcher : Matcher<T>
        {
            private readonly Matcher<T> matcher;

            public RepeatMatcher(Matcher<T> matcher)
            {
                this.matcher = matcher;
            }

            public override bool TryMatch(IList<T> sequence, ref int index)
            {
                while (matcher.TryMatch(sequence, ref index))
                {
                }

                return true;
            }

            public override string ToString()
            {
                return string.Format("({0}*)", matcher);
            }
        }
    }
}