// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Telemetry;

namespace Microsoft.VisualStudio.Telemetry
{
    internal interface IFaultUtility
    {
        void AddProcessDump(int pid);
        void AddFile(string fullpathname);
    }
}

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    /// <summary>
    /// dummy types just to make linked file work
    /// </summary>
    internal class WatsonReporter
    {
        public static void Report(string description, Exception exception, WatsonSeverity severity = WatsonSeverity.Default)
        {
            // do nothing
        }

        public static void Report(string description, Exception exception, Func<IFaultUtility, int> callback, WatsonSeverity severity = WatsonSeverity.Default)
        {
            // do nothing
        }
    }

    internal enum WatsonSeverity
    {
        /// <summary>
        /// Indicate that this watson is informative and not urgent
        /// </summary>
        Default,

        /// <summary>
        /// Indicate that this watson is critical and need to be addressed soon
        /// </summary>
        Critical,
    }
}
