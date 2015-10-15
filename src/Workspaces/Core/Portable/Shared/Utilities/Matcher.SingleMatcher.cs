// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            {
                return _description;
            }
        }
    }
}
