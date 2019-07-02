// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ExceptionHelpersTests : TestBase
    {
        /// <summary>
        /// Test that throwing OperationCanceledException does NOT trigger FailFast
        /// </summary>
        [Fact]
        public void TestExecuteWithErrorReportingThrowOperationCanceledException()
        {
            var finallyExecuted = false;

            void a()
            {
                try
                {
                    throw new OperationCanceledException();
                }
                finally
                {
                    finallyExecuted = true;
                }
            }

            try
            {
                try
                {
                    a();
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }

                Assert.True(false, "Should not get here because an exception should be thrown before this point.");
            }
            catch (OperationCanceledException)
            {
                Assert.True(finallyExecuted);
                return;
            }

            Assert.True(false, "Should have returned in the catch block before this point.");
        }
    }
}
