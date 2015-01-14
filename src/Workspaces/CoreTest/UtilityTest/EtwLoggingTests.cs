// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class LoggingTests : TestBase
    {
        [Fact]
        public void TestAllocationPooling()
        {
            Logger.SetLogger(AggregateLogger.Create(TestLogger.Instance, Logger.GetLogger()));

            var block1 = LogBlock();
            var block2 = LogBlock();
            Assert.NotEqual(block1, block2);

            block1.Dispose();
            var block3 = LogBlock();
            Assert.Same(block1, block3);

            block2.Dispose();
            block3.Dispose();
        }

        private static IDisposable LogBlock()
        {
            return Logger.LogBlock(FunctionId.TestEvent_NotUsed, CancellationToken.None);
        }

        private class TestLogger : ILogger
        {
            public static readonly ILogger Instance = new TestLogger();

            public bool IsEnabled(FunctionId functionId)
            {
                return true;
            }

            public void Log(FunctionId functionId, LogMessage logMessage) { }
            public void LogBlockStart(FunctionId functionId, LogMessage logMessage, int uniquePairId, CancellationToken cancellationToken) { }
            public void LogBlockEnd(FunctionId functionId, LogMessage logMessage, int uniquePairId, int delta, CancellationToken cancellationToken) { }
        }
    }
}
