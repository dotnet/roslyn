﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [SuppressMessage("Usage", "VSTHRD104:Offer async methods", Justification = "This class tests specific behavior of tasks.")]
    public class SpecializedTasksTests
    {
        [Fact]
        public void WhenAll_Null()
        {
#pragma warning disable CA2012 // Use ValueTasks correctly (the instance is never created)
            Assert.Throws<ArgumentNullException>(() => SpecializedTasks.WhenAll<int>(null));
#pragma warning restore CA2012 // Use ValueTasks correctly
        }

        [Fact]
        public void WhenAll_Empty()
        {
            var whenAll = SpecializedTasks.WhenAll(SpecializedCollections.EmptyEnumerable<ValueTask<int>>());
            Debug.Assert(whenAll.IsCompleted);
            Assert.True(whenAll.IsCompletedSuccessfully);
            Assert.Same(Array.Empty<int>(), whenAll.Result);
        }

        [Fact]
        public void WhenAll_AllCompletedSuccessfully()
        {
            var whenAll = SpecializedTasks.WhenAll(new[] { ValueTaskFactory.FromResult(0), ValueTaskFactory.FromResult(1) });
            Debug.Assert(whenAll.IsCompleted);
            Assert.True(whenAll.IsCompletedSuccessfully);
            Assert.Equal(new[] { 0, 1 }, whenAll.Result);
        }

        [Fact]
        public void WhenAll_CompletedButCanceled()
        {
            var whenAll = SpecializedTasks.WhenAll(new[] { new ValueTask<int>(Task.FromCanceled<int>(new CancellationToken(true))) });
            Assert.True(whenAll.IsCompleted);
            Assert.False(whenAll.IsCompletedSuccessfully);
            Assert.ThrowsAsync<OperationCanceledException>(async () => await whenAll);
        }

        [Fact]
        public void WhenAll_NotYetCompleted()
        {
            var completionSource = new TaskCompletionSource<int>();
            var whenAll = SpecializedTasks.WhenAll(new[] { new ValueTask<int>(completionSource.Task) });
            Assert.False(whenAll.IsCompleted);
            completionSource.SetResult(0);
            Assert.True(whenAll.IsCompleted);
            Debug.Assert(whenAll.IsCompleted);
            Assert.Equal(new[] { 0 }, whenAll.Result);
        }
    }
}
