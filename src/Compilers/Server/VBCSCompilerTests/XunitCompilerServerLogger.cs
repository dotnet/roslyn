// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CommandLine;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class XunitCompilerServerLogger : ICompilerServerLogger
    {
        public ITestOutputHelper TestOutputHelper { get; }
        public bool IsLogging => true;
        public List<string>? CapturedLogs { get; }

        public XunitCompilerServerLogger(ITestOutputHelper testOutputHelper, bool captureLogs = false)
        {
            TestOutputHelper = testOutputHelper;
            if (captureLogs)
            {
                CapturedLogs = new List<string>();
            }
        }

        public void Log(string message)
        {
            TestOutputHelper.WriteLine(message);
            CapturedLogs?.Add(message);
        }
    }
}
