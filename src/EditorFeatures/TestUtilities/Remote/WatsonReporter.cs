// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.ErrorReporting
{
    /// <summary>
    /// Mock to make test project build
    /// </summary>
    internal class WatsonReporter
    {
        public static void Report(string _1, Exception _2)
        {
            // do nothing
        }

        public static void Report(string _1, Exception _2, Func<IFaultUtility, int> _3)
        {
            // do nothing
        }

        public interface IFaultUtility
        {
            void AddProcessDump(int pid);
            void AddFile(string fullpathname);
        }
    }
}
