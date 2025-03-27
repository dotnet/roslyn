// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Roslyn.Test.Utilities
{
    public static class OSVersion
    {
        /// <summary>
        /// True when the operating system is at least Windows version 8
        /// </summary>
        public static bool IsWin8 =>
            System.Environment.OSVersion.Version.Build >= 9200;
    }
}
