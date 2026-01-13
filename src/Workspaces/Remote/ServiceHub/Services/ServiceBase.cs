// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Roslyn.Utilities;

#pragma warning disable CS0618 // Type or member is obsolete - this should become error once we provide infra for migrating to ISB (https://github.com/dotnet/roslyn/issues/44326)

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Base type with servicehub helper methods. this is not tied to how Roslyn OOP works. 
    /// 
    /// any type that derived from this type is supposed to be an entry point for servicehub services.
    /// name of the type should match one appears in GenerateServiceHubConfigurationFiles.targets 
    /// and signature of either its constructor or static CreateAsync must follow the convension
    /// ctor(Stream stream, IServiceProvider serviceProvider).
    /// 
    /// see servicehub detail from VSIDE onenote
    /// https://microsoft.sharepoint.com/teams/DD_VSIDE
    /// </summary>
    internal abstract class ServiceBase : IDisposable
    {
        private static int s_instanceId;

        protected readonly RemoteEndPoint EndPoint;
        protected readonly int InstanceId;
        protected readonly TraceSource Logger;

        protected readonly RemoteWorkspaceManager WorkspaceManager;

        // test data are only available when running tests:
        internal readonly RemoteHostTestData? TestData;

        static ServiceBase()
        {
            WatsonReporter.InitializeFatalErrorHandlers();
        }

        protected ServiceBase(IServiceProvider serviceProvider, Stream stream, IEnumerable<JsonConverter>? jsonConverters = null)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);

            TestData = (RemoteHostTestData?)serviceProvider.GetService(typeof(RemoteHostTestData));
            WorkspaceManager = TestData?.WorkspaceManager ?? RemoteWorkspaceManager.Default;

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Log(TraceEventType.Information, "Service instance created");

            // invoke all calls incoming over the stream on this service instance:
            EndPoint = new RemoteEndPoint(stream, Logger, incomingCallTarget: this, jsonConverters);
        }

        public void Dispose()
        {
            if (EndPoint.IsDisposed)
            {
                // guard us from double disposing. this can happen in unit test
                // due to how we create test mock service hub stream that tied to
                // remote host service
                return;
            }

            EndPoint.Dispose();

            Log(TraceEventType.Information, "Service instance disposed");
        }

        protected void StartService()
        {
            EndPoint.StartListening();
        }

        protected string DebugInstanceString => $"{GetType()} ({InstanceId})";

        protected void Log(TraceEventType errorType, string message)
            => Logger.TraceEvent(errorType, 0, $"{DebugInstanceString}: {message}");

        public RemoteWorkspace GetWorkspace()
            => WorkspaceManager.GetWorkspace();

        protected Task<Solution> GetSolutionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            var assetProvider = workspace.CreateAssetProvider(solutionInfo, WorkspaceManager.SolutionAssetCache, WorkspaceManager.GetAssetSource());
            return workspace.GetSolutionAsync(assetProvider, solutionInfo.SolutionChecksum, solutionInfo.FromPrimaryBranch, solutionInfo.WorkspaceVersion, cancellationToken).AsTask();
        }

        internal Task<Solution> GetSolutionImplAsync(JObject solutionInfo, CancellationToken cancellationToken)
        {
            var reader = solutionInfo.CreateReader();
            var serializer = JsonSerializer.Create(new JsonSerializerSettings() { Converters = new[] { AggregateJsonConverter.Instance }, DateParseHandling = DateParseHandling.None });
            var pinnedSolutionInfo = serializer.Deserialize<PinnedSolutionInfo>(reader)
                ?? throw new InvalidOperationException("Failed to deserialize json reader.");

            return GetSolutionAsync(pinnedSolutionInfo, cancellationToken);
        }

        protected async Task<T> RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();

            try
            {
                return await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected async Task RunServiceAsync(Func<Task> callAsync, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();

            try
            {
                await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected T RunService<T>(Func<T> call, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();

            try
            {
                return call();
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected void RunService(Action call, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();

            try
            {
                call();
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
