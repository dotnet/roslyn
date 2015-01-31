// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    internal partial class Matcher<T>
    {
        private class ChoiceMatcher : Matcher<T>
        {
            private readonly Matcher<T> _matcher1;
            private readonly Matcher<T> _matcher2;

            public ChoiceMatcher(Matcher<T> matcher1, Matcher<T> matcher2)
            {
                _matcher1 = matcher1;
                _matcher2 = matcher2;
            }

            public override bool TryMatch(IList<T> sequence, ref int index)
            {
                return
                    _matcher1.TryMatch(sequence, ref index) ||
                    _matcher2.TryMatch(sequence, ref index);
            }

            public override string ToString()
            {
                return string.Format("({0}|{1})", _matcher1, _matcher2);
            }
        }
    }
}
