// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    /// <summary>
    /// Compares string based upon their ordinal equality.
    /// </summary>
    internal class StringOrdinalComparer : IEqualityComparer<string>
    {
        public static readonly StringOrdinalComparer Instance = new StringOrdinalComparer();

        private StringOrdinalComparer()
        {
        }

        bool IEqualityComparer<string>.Equals(string a, string b)
        {
            return StringOrdinalComparer.Equals(a, b);
        }

        public static bool Equals(string a, string b)
        {
            // this is fast enough
            return string.Equals(a, b);
        }

        int IEqualityComparer<string>.GetHashCode(string s)
        {
            // PERF: default string hashcode is not always good or fast,
            // but we can use anything we want in our dictionaries. 
            // Our typical scenario is a relatively short string (identifier)
            // FNV seems good enough for such cases
            return Hash.GetFNVHashCode(s);
        }
    }
}
