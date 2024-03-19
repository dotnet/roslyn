// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable();
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
