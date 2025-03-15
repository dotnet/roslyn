// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Analyzer.Utilities.Extensions
{
    internal static class VersionExtension
    {
        public static bool IsGreaterThanOrEqualTo(this Version? current, Version? compare)
        {
            if (current == null)
            {
                return compare == null;
            }

            if (compare == null)
            {
                return true;
            }

            if (current.Major != compare.Major)
            {
                return current.Major > compare.Major;
            }

            if (current.Minor != compare.Minor)
            {
                return current.Minor > compare.Minor;
            }

            // For build or revision value of 0 equals to -1
            if (current.Build != compare.Build && (current.Build > 0 || compare.Build > 0))
            {
                return current.Build > compare.Build;
            }

            if (current.Revision != compare.Revision && (current.Revision > 0 || compare.Revision > 0))
            {
                return current.Revision > compare.Revision;
            }

            return true;
        }
    }
}
