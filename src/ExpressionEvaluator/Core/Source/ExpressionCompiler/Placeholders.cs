// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
