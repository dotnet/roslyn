// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Roslyn.Test.Utilities
{
    public static class RegexExtensions
    {
        public static IEnumerable<Match> ToEnumerable(this MatchCollection collection)
        {
            foreach (Match m in collection)
            {
                yield return m;
            }
        }
    }
}
