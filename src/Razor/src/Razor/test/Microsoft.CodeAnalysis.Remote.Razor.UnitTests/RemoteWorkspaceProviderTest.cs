// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public class RemoteWorkspaceProviderTest
{
    [Fact]
    public async Task InitializeRemoteExportProviderBuilderAsync_OnlyInitializesOnce()
    {
        var callCount = 0;
        var initializationStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowInitializationToComplete = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        RemoteWorkspaceProvider.TestAccessor.ResetInitializeRemoteExportProviderBuilder();

        try
        {
            RemoteWorkspaceProvider.TestAccessor.SetInitializeRemoteExportProviderBuilder(async (_, _, _) =>
            {
                Interlocked.Increment(ref callCount);
                initializationStarted.TrySetResult(null);
                await allowInitializationToComplete.Task.ConfigureAwait(false);
                return "test-error";
            });

            var traceSource = new TraceSource(nameof(RemoteWorkspaceProviderTest));
            var firstTask = RemoteWorkspaceProvider.TestAccessor.InitializeRemoteExportProviderBuilderAsync("test", traceSource, CancellationToken.None);

            await initializationStarted.Task;

            var secondTask = RemoteWorkspaceProvider.TestAccessor.InitializeRemoteExportProviderBuilderAsync("test", traceSource, CancellationToken.None);
            Assert.False(secondTask.IsCompleted);

            allowInitializationToComplete.TrySetResult(null);

            var results = await Task.WhenAll(firstTask, secondTask);

            Assert.Equal(1, callCount);
            Assert.Collection(results,
                result => Assert.Equal("test-error", result),
                result => Assert.Equal("test-error", result));
        }
        finally
        {
            RemoteWorkspaceProvider.TestAccessor.ResetInitializeRemoteExportProviderBuilder();
        }
    }
}
