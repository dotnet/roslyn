// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
