// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class VersionTestHelpers
    {
        public static void GetDefautVersion(DateTime time, out int days, out int seconds)
        {
            days = (int)(time - new DateTime(2000, 1, 1)).TotalDays; // number of days since Jan 1, 2000
            seconds = (int)time.TimeOfDay.TotalSeconds / 2; // number of seconds since midnight divided by two
        }
    }
}
