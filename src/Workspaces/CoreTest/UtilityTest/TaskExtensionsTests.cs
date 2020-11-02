// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;
using TaskExtensions = Roslyn.Utilities.TaskExtensions;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class TaskExtensionsTests
    {
        [Fact]
        public void WaitAndGetResult()
        {
            Assert.Equal(42, Task.FromResult(42).WaitAndGetResult_CanCallOnBackground(CancellationToken.None));
            Assert.Throws<TaskCanceledException>(() => Task.FromCanceled<int>(new CancellationToken(canceled: true)).WaitAndGetResult_CanCallOnBackground(CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => new TaskCompletionSource<int>().Task.WaitAndGetResult_CanCallOnBackground(new CancellationToken(canceled: true)));
            var ex = Assert.Throws<TestException>(() => Task.Run(() => ThrowTestException()).WaitAndGetResult_CanCallOnBackground(CancellationToken.None));
            Assert.Contains($"{nameof(TaskExtensionsTests)}.{nameof(ThrowTestException)}()", ex.StackTrace);
        }

        [Fact]
        public void GetCompletedResultOfNullTaskTest()
        {
            Assert.Throws<ArgumentNullException>(() => TaskExtensions.GetCompletedResult<int>(null!));
        }

        [Fact]
        public void GetCompletedResultOfIncompleteTaskTest()
        {
            var tcs = new TaskCompletionSource<int>();
            Assert.Throws<InvalidOperationException>(() => TaskExtensions.GetCompletedResult(tcs.Task));
        }

        [Fact]
        public void GetCompletedResultOfCompleteTaskTest()
        {
            var expected = new object();
            Assert.Same(expected, TaskExtensions.GetCompletedResult(Task.FromResult(expected)));
        }

        [Fact]
        public void GetCompletedResultOfCancelledTaskTest()
        {
            var tcs = new TaskCompletionSource<int>();
            tcs.SetCanceled();

            // The test is run for Task<T>.GetAwaiter().GetResult() and TaskExceptions.CompletedResult to ensure consistent behavior
            TestBehavior(() => tcs.Task.GetAwaiter().GetResult());
            TestBehavior(() => TaskExtensions.GetCompletedResult(tcs.Task));

            // Local function
            static void TestBehavior(Func<object> testCode)
            {
                Assert.Throws<TaskCanceledException>(testCode);
            }
        }

        [Fact]
        public void GetCompletedResultOfFailedTaskTest()
        {
            var tcs = new TaskCompletionSource<int>();
            var expectedException = new EncoderFallbackException();
            tcs.SetException(expectedException);

            // The test is run for Task<T>.GetAwaiter().GetResult() and TaskExceptions.CompletedResult to ensure consistent behavior
            TestBehavior(() => tcs.Task.GetAwaiter().GetResult());
            TestBehavior(() => TaskExtensions.GetCompletedResult(tcs.Task));

            // Local function
            void TestBehavior(Func<object> testCode)
            {
                Assert.Same(expectedException, Assert.Throws<EncoderFallbackException>(testCode));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ThrowTestException() => throw new TestException();

        private class TestException : Exception { }
    }
}
