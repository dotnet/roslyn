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
        private static void AssertThatActionFailsFast(Action a)
        {
            Action wrappedAction = () =>
            {
                try
                {
                    a();
                    Assert.True(false, "Should not get here because we expect the action to throw an exception.");
                }
                catch (Exception)
                {
                    Assert.True(false, "Should not get here because FailFast should prevent entering the catch block.");
                }
                finally
                {
                    Assert.True(false, "Should not get here because FailFast should prevent running finalizers.");
                }
            };

            // TODO: How can we test that FailFast works when it will bring down the process?
            // One idea is to run the action in a sacrificial process and check the exit code.
            // We would need to disable Watson for that process.
        }

        /// <summary>
        /// Test that throwing OperationCanceledException does NOT trigger FailFast
        /// </summary>
        [Fact]
        public void TestExecuteWithErrorReportingThrowOperationCanceledException()
        {
            bool finallyExecuted = false;

            Action a = () =>
            {
                try
                {
                    throw new OperationCanceledException();
                }
                finally
                {
                    finallyExecuted = true;
                }
            };

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
        [Fact]
        public void TestExecuteWithErrorReportingWithSuppressFailFast()
        {
            bool finallyExecuted = false;

            Action a = () =>
            {
                try
                {
                    throw new ArgumentOutOfRangeException();
                }
                finally
                {
                    finallyExecuted = true;
                }
            };

            try
            {
                using (ExceptionHelpers.SuppressFailFast())
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
            }
            catch (ArgumentOutOfRangeException)
            {
                Assert.True(finallyExecuted);
            }

            Assert.False(ExceptionHelpers.IsFailFastSuppressed());
        }
    }
}
