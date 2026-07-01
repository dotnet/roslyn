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
        private readonly object _messagesGate = new object();

        public ITestOutputHelper TestOutputHelper { get; }
        public List<string> Messages { get; } = new List<string>();
        public bool IsLogging => true;

        public XunitCompilerServerLogger(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = testOutputHelper;
        }

        public void Log(string message)
        {
            lock (_messagesGate)
            {
                Messages.Add(message);
            }

            TestOutputHelper.WriteLine(message);
        }
    }
}
