// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class VersionTestHelpers
    {
        public static void GetDefaultVersion(DateTime time, out int days, out int seconds)
        {
            days = (int)(time - new DateTime(2000, 1, 1)).TotalDays; // number of days since Jan 1, 2000
            seconds = (int)time.TimeOfDay.TotalSeconds / 2; // number of seconds since midnight divided by two
        }
    }
}
