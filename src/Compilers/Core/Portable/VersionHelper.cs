// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis
{
    internal static class VersionHelper
    {
        /// <summary>
        /// Parses a version string of the form "major [ '.' minor [ '.' build [ '.' revision ] ] ]".
        /// </summary>
        /// <param name="s">The version string to parse.</param>
        /// <param name="version">If parsing succeeds, the parsed version. Null otherwise.</param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        internal static bool TryParse(string s, out Version version)
        {
            return TryParse(s, allowWildcard: false, maxValue: ushort.MaxValue, version: out version);
        }

        /// <summary>
        /// Parses a version string of the form "major [ '.' minor [ '.' ( '*' | ( build [ '.' ( '*' | revision ) ] ) ) ] ]"
        /// as accepted by System.Reflection.AssemblyVersionAttribute.
        /// </summary>
        /// <param name="s">The version string to parse.</param>
        /// <param name="allowWildcard">Indicates whether or not a wildcard is accepted as the terminal component.</param>
        /// <param name="version">
        /// If parsing succeeded, the parsed version. Null otherwise.
        /// If <paramref name="s"/> contains * the version build and/or revision numbers are set to <see cref="ushort.MaxValue"/>.
        /// </param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        internal static bool TryParseAssemblyVersion(string s, bool allowWildcard, out Version version)
        {
            return TryParse(s, allowWildcard: allowWildcard, maxValue: ushort.MaxValue - 1, version: out version);
        }

        /// <summary>
        /// Parses a version string of the form "major [ '.' minor [ '.' ( '*' | ( build [ '.' ( '*' | revision ) ] ) ) ] ]"
        /// as accepted by System.Reflection.AssemblyVersionAttribute.
        /// </summary>
        /// <param name="s">The version string to parse.</param>
        /// <param name="allowWildcard">Indicates whether or not we're parsing an assembly version string. If so, wildcards are accepted and each component must be less than 65535.</param>
        /// <param name="maxValue">The maximum value that a version component may have.</param>
        /// <param name="version">
        /// If parsing succeeded, the parsed version. Null otherwise. 
        /// If <paramref name="s"/> contains * and wildcard is allowed the version build and/or revision numbers are set to <see cref="ushort.MaxValue"/>.
        /// </param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        private static bool TryParse(string s, bool allowWildcard, ushort maxValue, out Version version)
        {
            Debug.Assert(!allowWildcard || maxValue < ushort.MaxValue);

            if (s == null)
            {
                version = null;
                return false;
            }

            string[] elements = s.Split('.');

            // If the wildcard is being used, the first two elements must be specified explicitly, and
            // the last must be a exactly single asterisk without whitespace.
            bool hasWildcard = allowWildcard && elements[elements.Length - 1] == "*";

            if ((hasWildcard && elements.Length < 3) || elements.Length > 4)
            {
                version = null;
                return false;
            }

            ushort[] values = new ushort[4];
            int lastExplicitValue = hasWildcard ? elements.Length - 1 : elements.Length;
            for (int i = 0; i < lastExplicitValue; i++)
            {
                if (!ushort.TryParse(elements[i], NumberStyles.None, CultureInfo.InvariantCulture, out values[i]) || values[i] > maxValue)
                {
                    version = null;
                    return false;
                }
            }

            if (hasWildcard)
            {
                for (int i = lastExplicitValue; i < values.Length; i++)
                {
                    values[i] = ushort.MaxValue;
                }
            }

            version = new Version(values[0], values[1], values[2], values[3]);
            return true;
        }

        /// <summary>
        /// If build and/or revision numbers are 65535 they are replaced with time-based values.
        /// </summary>
        public static Version GenerateVersionFromPatternAndCurrentTime(DateTime time, Version pattern)
        {
            if (pattern == null || pattern.Revision != ushort.MaxValue)
            {
                return pattern;
            }

            // MSDN doc on the attribute: 
            // "The default build number increments daily. The default revision number is the number of seconds since midnight local time 
            // (without taking into account time zone adjustments for daylight saving time), divided by 2."
            if (time == default(DateTime))
            {
                time = DateTime.Now;
            }

            int revision = (int)time.TimeOfDay.TotalSeconds / 2;

            // 24 * 60 * 60 / 2 = 43200 < 65535
            Debug.Assert(revision < ushort.MaxValue);

            if (pattern.Build == ushort.MaxValue)
            {
                TimeSpan days = time.Date - new DateTime(2000, 1, 1);
                int build = Math.Min(ushort.MaxValue, (int)days.TotalDays);

                return new Version(pattern.Major, pattern.Minor, (ushort)build, (ushort)revision);
            }
            else
            {
                return new Version(pattern.Major, pattern.Minor, pattern.Build, (ushort)revision);
            }
        }
    }
}
