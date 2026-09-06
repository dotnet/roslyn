// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Testing;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.Remote;

public sealed partial class ServiceHubServicesTests
{
    /// <summary>
    /// The Performance Loggers options page pushes diagnostic logger enablement into the OOP process
    /// over this API. Covers that the remote host registers a sink while it is enabled and unregisters
    /// it when it is not, observed through whether anything is listening for a given
    /// <see cref="FunctionId"/> - which is also what makes logging free when nothing is.
    /// </summary>
    [Fact]
    public async Task TestRemoteProcessEnableLogging()
    {
        using var workspace = CreateWorkspace();
        using var client = await InProcRemoteHostClient.GetTestClientAsync(workspace).ConfigureAwait(false);

        Assert.False(AnythingIsListening(FunctionId.TestEvent_NotUsed));

        Assert.True(await SetRemoteLoggingAsync([nameof(TraceEventSink)], [FunctionId.TestEvent_NotUsed]));
        Assert.True(AnythingIsListening(FunctionId.TestEvent_NotUsed));

        // The sink was built with a predicate covering only the requested ids.
        Assert.False(AnythingIsListening(FunctionId.RemoteHost_Connect));

        Assert.True(await SetRemoteLoggingAsync([], []));
        Assert.False(AnythingIsListening(FunctionId.TestEvent_NotUsed));

        Task<bool> SetRemoteLoggingAsync(ImmutableArray<string> loggerTypeNames, ImmutableArray<FunctionId> functionIds)
            => client.TryInvokeAsync<IRemoteProcessTelemetryService>(
                (service, cancellationToken) => service.EnableLoggingAsync(loggerTypeNames, functionIds, cancellationToken),
                CancellationToken.None).AsTask();

        // RoslynTelemetry only builds a message when some sink is enabled for the id, so the message
        // factory running is exactly "a sink is registered and wants this id".
        static bool AnythingIsListening(FunctionId functionId)
        {
            var listening = false;
            RoslynTelemetry.Log(functionId, () =>
            {
                listening = true;
                return string.Empty;
            });

            return listening;
        }
    }
}
