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
        public List<string> CapturedLogs { get; } = new List<string>();

        public XunitCompilerServerLogger(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        public void Log(string message)
        {
            TestOutputHelper.WriteLine(message);
            CapturedLogs.Add(message);
        }

        public void LogWarning(string message)
        {
            var warningMessage = $"Warning: {message}";
            TestOutputHelper.WriteLine(warningMessage);
            CapturedLogs.Add(warningMessage);
        }
    }
}
