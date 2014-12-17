// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class AsyncQueueTests
    {
        /// <summary>
        /// Ensure that cancel after completion does not cause an exception to be thrown.
        /// </summary>
        [Fact]
        [WorkItem(1097123, "DevDiv")]
        public async Task CancelAfterCompleted()
        {
            var cts = new CancellationTokenSource();
            var queue = new AsyncQueue<int>(cts.Token);
            queue.Complete();
            await queue.WhenCompletedAsync.ConfigureAwait(false);
            Assert.Equal(TaskStatus.RanToCompletion, queue.WhenCompletedAsync.Status);
            cts.Cancel();
            Assert.Equal(TaskStatus.RanToCompletion, queue.WhenCompletedAsync.Status);
        }
    }
}
