// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq
{
    internal static class LinqExtensions
    {
        public static bool All(this string s, Func<char, bool> predicate)
        {
            foreach (var ch in s)
            {
                if (!predicate(ch))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
