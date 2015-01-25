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
        /// <param name="version">If parsing succeeded, the parsed version. Null otherwise.</param>
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
        /// <param name="version">If parsing succeeded, the parsed version. Null otherwise.</param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        private static bool TryParse(string s, bool allowWildcard, ushort maxValue, out Version version)
        {
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
                int seconds = ((int)(DateTime.Now.TimeOfDay.TotalSeconds)) / 2;
                if (seconds > (int)maxValue)
                {
                    version = null;
                    return false;
                }
                values[3] = (ushort)seconds;

                if (elements.Length == 3)
                {
                    TimeSpan days = DateTime.Today - new DateTime(2000, 1, 1);
                    int build = (int)days.TotalDays;

                    if (build < 0 || build > (int)maxValue)
                    {
                        //alink would generate an error here saying "Cannot auto-generate build and 
                        //revision version numbers for dates previous to January 1, 2000." Without
                        //some refactoring here to relay the date problem, Roslyn
                        //will generate an inaccurate error about the version string being of the wrong format.

                        version = null;
                        return false;
                    }
                    values[2] = (ushort)build;
                }
            }

            version = new Version(values[0], values[1], values[2], values[3]);
            return true;
        }
    }
}
