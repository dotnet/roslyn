// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.CodeAnalysis
{
    internal static class VersionHelper
    {
        internal static bool Validate(ref Version v)
        {
            Debug.Assert(v != null);

            if (v.Major >= UInt16.MaxValue ||
                v.Minor >= UInt16.MaxValue ||
                v.Build >= UInt16.MaxValue ||
                v.Revision >= UInt16.MaxValue)
            {
                v = null;
                return false;
            }

            return true;
        }

        /// <summary>
        /// sets as many fields as it can.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="version"></param>
        /// <returns>True when the input was entirely parsable.</returns>
        internal static bool TryParse(string s, out Version version)
        {
            if (s == null)
            {
                version = new Version(0, 0, 0, 0);
                return false;
            }

            string[] elements = s.Split('.');
            ushort[] values = new ushort[4];
            bool result = elements.Length <= 4;

            for (int i = 0; i < elements.Length && i < 4; i++)
            {
                if (!UInt16.TryParse(elements[i], NumberStyles.None, CultureInfo.InvariantCulture, out values[i]))
                {
                    result = false;
                    break;
                }
            }

            version = new Version(values[0], values[1], values[2], values[3]);
            return result;
        }

        internal static bool TryParseWithWildcards(string s, out Version version)
        {
            if (s == null)
            {
                version = null;
                return false;
            }

            int indexOfSplat = s.LastIndexOf('*');
            if (indexOfSplat == -1)
            {
                if (VersionHelper.TryParse(s, out version))
                {
                    return Validate(ref version);
                }
                else
                {
                    version = null;
                    return false;
                }
            }
            else
            {
                string[] elements = s.Split('.');

                //to use the wildcard, the first two elements must be specified explicitly, and
                //the last one has got to be a single asterisk w/o whitespace.
                if (elements.Length < 3 ||
                    elements.Length > 4 ||
                    elements[elements.Length - 1] != "*")
                {
                    version = null;
                    return false;
                }

                string beforeSplat = s.Substring(0, indexOfSplat - 1);  //take off the splat and the preceding dot

                if (VersionHelper.TryParse(beforeSplat, out version))
                {
                    //the explicitly set portions were good
                    int seconds = ((int)(DateTime.Now.TimeOfDay.TotalSeconds)) / 2;
                    int build = version.Build;

                    if (elements.Length == 3)
                    {
                        TimeSpan days = DateTime.Today - new DateTime(2000, 1, 1);
                        build = (int)days.TotalDays;

                        if (build < 0)
                        {
                            //alink would generate an error here saying "Cannot auto-generate build and 
                            //revision version numbers for dates previous to January 1, 2000." Without
                            //some refactoring here to relay the date problem, Roslyn
                            //will generate an inaccurate error about the version string being of the wrong format.

                            version = null;
                            return false;
                        }
                    }

                    version = new Version(version.Major, version.Minor, build, seconds);
                    return Validate(ref version);
                }
                else
                {
                    version = null;
                    return false;
                }
            }
        }
    }
}
