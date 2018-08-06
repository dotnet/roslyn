// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int ThrowTestException() => throw new TestException();

        class TestException : Exception { }
    }
}
