using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.Cci
{
    /// <summary>
    /// A container for static helper methods that are used to test identities for equality.
    /// </summary>
    internal static class ObjectModelHelper
    {
        /// <summary>
        /// Returns a hash code based on the string content. Strings that differ only in case will always have the same hash code.
        /// </summary>
        /// <param name="s">The string to hash.</param>
        public static int CaseInsensitiveStringHash(string s)
        {
            int hashCode = 0;
            for (int i = 0, n = s.Length; i < n; i++)
            {
                char ch = s[i];
                ch = Char.ToLower(ch, CultureInfo.InvariantCulture);
                hashCode = hashCode * 17 + ch;
            }

            return hashCode;
        }
    }
}