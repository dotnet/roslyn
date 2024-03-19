// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// <param name="version">If parsing succeeds, the parsed version. Otherwise a version that represents as much of the input as could be parsed successfully.</param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        internal static bool TryParse(string s, out Version version)
        {
            return TryParse(s, allowWildcard: false, maxValue: ushort.MaxValue, allowPartialParse: true, version: out version);
        }

        /// <summary>
        /// Parses a version string of the form "major [ '.' minor [ '.' ( '*' | ( build [ '.' ( '*' | revision ) ] ) ) ] ]"
        /// as accepted by System.Reflection.AssemblyVersionAttribute.
        /// </summary>
        /// <param name="s">The version string to parse.</param>
        /// <param name="allowWildcard">Indicates whether or not a wildcard is accepted as the terminal component.</param>
        /// <param name="version">
        /// If parsing succeeded, the parsed version. Otherwise a version instance with all parts set to zero.
        /// If <paramref name="s"/> contains * the version build and/or revision numbers are set to <see cref="ushort.MaxValue"/>.
        /// </param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        internal static bool TryParseAssemblyVersion(string s, bool allowWildcard, out Version version)
        {
            return TryParse(s, allowWildcard: allowWildcard, maxValue: ushort.MaxValue - 1, allowPartialParse: false, version: out version);
        }

        /// <summary>
        /// Parses a version string of the form "major [ '.' minor [ '.' ( '*' | ( build [ '.' ( '*' | revision ) ] ) ) ] ]"
        /// as accepted by System.Reflection.AssemblyVersionAttribute.
        /// </summary>
        /// <param name="s">The version string to parse.</param>
        /// <param name="allowWildcard">Indicates whether or not we're parsing an assembly version string. If so, wildcards are accepted and each component must be less than 65535.</param>
        /// <param name="maxValue">The maximum value that a version component may have.</param>
        /// <param name="allowPartialParse">Allow the parsing of version elements where invalid characters exist. e.g. 1.2.2a.1</param>
        /// <param name="version">
        /// If parsing succeeded, the parsed version. When <paramref name="allowPartialParse"/> is true a version with values up to the first invalid character set. Otherwise a version with all parts set to zero.
        /// If <paramref name="s"/> contains * and wildcard is allowed the version build and/or revision numbers are set to <see cref="ushort.MaxValue"/>.
        /// </param>
        /// <returns>True when parsing succeeds completely (i.e. every character in the string was consumed), false otherwise.</returns>
        private static bool TryParse(string s, bool allowWildcard, ushort maxValue, bool allowPartialParse, out Version version)
        {
            Debug.Assert(!allowWildcard || maxValue < ushort.MaxValue);

            if (string.IsNullOrWhiteSpace(s))
            {
                version = AssemblyIdentity.NullVersion;
                return false;
            }

            string[] elements = s.Split('.');

            // If the wildcard is being used, the first two elements must be specified explicitly, and
            // the last must be a exactly single asterisk without whitespace.
            bool hasWildcard = allowWildcard && elements[elements.Length - 1] == "*";

            if ((hasWildcard && elements.Length < 3) || elements.Length > 4)
            {
                version = AssemblyIdentity.NullVersion;
                return false;
            }

            ushort[] values = new ushort[4];
            int lastExplicitValue = hasWildcard ? elements.Length - 1 : elements.Length;
            bool parseError = false;
            for (int i = 0; i < lastExplicitValue; i++)
            {

                if (!ushort.TryParse(elements[i], NumberStyles.None, CultureInfo.InvariantCulture, out values[i]) || values[i] > maxValue)
                {
                    if (!allowPartialParse)
                    {
                        version = AssemblyIdentity.NullVersion;
                        return false;
                    }

                    parseError = true;

                    if (string.IsNullOrWhiteSpace(elements[i]))
                    {
                        values[i] = 0;
                        break;
                    }

                    if (values[i] > maxValue)
                    {
                        //The only way this can happen is if the value was 65536
                        //The old compiler would continue parsing from here
                        values[i] = 0;
                        continue;
                    }

                    bool invalidFormat = false;
                    System.Numerics.BigInteger number = 0;

                    //There could be an invalid character in the input so check for the presence of one and
                    //parse up to that point. examples of invalid characters are alphas and punctuation
                    for (var idx = 0; idx < elements[i].Length; idx++)
                    {
                        if (!char.IsDigit(elements[i][idx]))
                        {
                            invalidFormat = true;

                            TryGetValue(elements[i].Substring(0, idx), out values[i]);
                            break;
                        }
                    }

                    if (!invalidFormat)
                    {
                        //if we made it here then there weren't any alpha or punctuation chars in the input so the
                        //element is either greater than ushort.MaxValue or possibly a fullwidth unicode digit.
                        if (TryGetValue(elements[i], out values[i]))
                        {
                            //For this scenario the old compiler would continue processing the remaining version elements
                            //so continue processing
                            continue;
                        }
                    }

                    //Don't process any more of the version elements
                    break;
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
            return !parseError;
        }

        private static bool TryGetValue(string s, out ushort value)
        {
            System.Numerics.BigInteger number;
            if (System.Numerics.BigInteger.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out number))
            {
                //The old compiler would take the 16 least significant bits and use their value as the output
                //so we'll do that too.
                value = (ushort)(number % 65536);
                return true;
            }

            //One case that will cause us to end up here is when the input is a Fullwidth unicode digit
            //so we'll always return zero
            value = 0;
            return false;
        }

        /// <summary>
        /// If build and/or revision numbers are 65535 they are replaced with time-based values.
        /// </summary>
        public static Version? GenerateVersionFromPatternAndCurrentTime(DateTime time, Version pattern)
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
