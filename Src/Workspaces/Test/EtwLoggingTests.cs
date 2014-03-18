// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class EtwLoggingTests : TestBase
    {
        [Fact]
        public void TestAllocationPooling()
        {
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
            return EtwLogger.Instance.LogBlock((FeatureId)(-1), FunctionId.TestEvent_NotUsed, "", 0, CancellationToken.None);
        }
    }
}
