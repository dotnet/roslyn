// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.CommandLine;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities
{
    internal sealed class TestableCompilerServerLogger : ICompilerServerLogger
    {
        public bool IsLogging { get; set; }
        public Action<string> LogFunc { get; set; } = delegate { throw new InvalidOperationException(); };

        public void Log(string message) => LogFunc(message);
    }
}
