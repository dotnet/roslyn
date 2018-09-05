// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
