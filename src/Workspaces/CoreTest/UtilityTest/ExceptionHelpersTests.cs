// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class ExceptionHelpersTests : TestBase
    {
        private readonly Action<Exception> _fatalHandler;
        private readonly Action<Exception> _nonFatalHandler;

        public ExceptionHelpersTests()
        {
            _fatalHandler = FatalError.Handler;
            _nonFatalHandler = FatalError.NonFatalHandler;
        }

        public override void Dispose()
        {
            FatalError.OverwriteHandler(_fatalHandler);
            FatalError.OverwriteNonFatalHandler(_nonFatalHandler);

            base.Dispose();
        }

        /// <summary>
        /// Test that throwing OperationCanceledException does NOT trigger FailFast
        /// </summary>
        [Fact]
        public void TestExecuteWithErrorReportingThrowOperationCanceledException()
        {
            bool finallyExecuted = false;

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

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/31014")]
        [WorkItem(31014, "https://github.com/dotnet/roslyn/issues/31014")]
        public void TestExecuteWithErrorReportingWithSuppressFailFast()
        {
            var failFastSuppressedInFilter = default(bool?);
            var fatalErrorHandlerCalled = false;
            var finallyExecuted = false;
            var expected = new ArgumentOutOfRangeException();

            FatalError.OverwriteHandler(_ => fatalErrorHandlerCalled = true);

            void a()
            {
                try
                {
                    throw expected;
                }
                finally
                {
                    finallyExecuted = true;
                }
            }

            bool VerifyFailFastSuppressed(Exception e)
            {
                failFastSuppressedInFilter = ExceptionHelpers.IsFailFastSuppressed();
                return FatalError.ReportUnlessCanceled(e);
            }

            var thrown = Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                using (ExceptionHelpers.SuppressFailFast())
                {
                    try
                    {
                        a();
                    }
                    catch (Exception e) when (VerifyFailFastSuppressed(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }
            });

            Assert.Same(expected, thrown);
            Assert.True(finallyExecuted);
            Assert.True(failFastSuppressedInFilter);
            Assert.False(fatalErrorHandlerCalled);
            Assert.False(ExceptionHelpers.IsFailFastSuppressed());
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/31014")]
        [WorkItem(31014, "https://github.com/dotnet/roslyn/issues/31014")]
        public async Task TestExecuteAsyncWithErrorReportingWithSuppressFailFast()
        {
            var failFastSuppressedInFilter = default(bool?);
            var fatalErrorHandlerCalled = false;
            var finallyExecuted = false;
            var expected = new ArgumentOutOfRangeException();

            FatalError.OverwriteHandler(_ => fatalErrorHandlerCalled = true);

            async Task a()
            {
                try
                {
                    await Task.Yield();
                    throw expected;
                }
                finally
                {
                    finallyExecuted = true;
                }
            }

            bool VerifyFailFastSuppressed(Exception e)
            {
                failFastSuppressedInFilter = ExceptionHelpers.IsFailFastSuppressed();
                return FatalError.ReportUnlessCanceled(e);
            }

            var thrown = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                using (ExceptionHelpers.SuppressFailFast())
                {
                    try
                    {
                        await a();
                    }
                    catch (Exception e) when (VerifyFailFastSuppressed(e))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }
            });

            Assert.Same(expected, thrown);
            Assert.True(finallyExecuted);
            Assert.True(failFastSuppressedInFilter);
            Assert.False(fatalErrorHandlerCalled);
            Assert.False(ExceptionHelpers.IsFailFastSuppressed());
        }
    }
}
