// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Boost performance of any servicehub service which is invoked by user explicit actions
    /// </summary>
    internal static class ProcessExtensions
    {
        private static bool s_settingPrioritySupported = true;

        public static bool TrySetPriorityClass(this Process process, ProcessPriorityClass priorityClass)
        {
            if (!s_settingPrioritySupported)
            {
                return false;
            }

            try
            {
                process.PriorityClass = priorityClass;
                return true;
            }
            catch (Exception e) when (e is PlatformNotSupportedException || e is Win32Exception)
            {
                // the runtime does not support changing process priority
                s_settingPrioritySupported = false;

                return false;
            }
        }
    }
}
