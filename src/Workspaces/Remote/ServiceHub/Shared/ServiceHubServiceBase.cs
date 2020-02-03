// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Newtonsoft.Json;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: all service hub service should be extract to interface so that it can support multiple hosts.
    //       right now, tightly coupled to service hub
    internal abstract class ServiceHubServiceBase : ServiceBase
    {
        /// <summary>
        /// PinnedSolutionInfo.ScopeId. scope id of the solution. caller and callee share this id which one
        /// can use to find matching caller and callee while exchanging data
        /// 
        /// PinnedSolutionInfo.FromPrimaryBranch Marks whether the solution checksum it got is for primary branch or not 
        /// 
        /// this flag will be passed down to solution controller to help
        /// solution service's cache policy. for more detail, see <see cref="SolutionService"/>
        /// 
        /// PinnedSolutionInfo.SolutionChecksum indicates solution this connection belong to
        /// </summary>
        private PinnedSolutionInfo? _solutionInfo;

        private RoslynServices? _lazyRoslynServices;

        // Used by Razor: https://github.com/aspnet/AspNetCore-Tooling/blob/master/src/Razor/src/Microsoft.CodeAnalysis.Remote.Razor/RazorServiceBase.cs
        protected ServiceHubServiceBase(IServiceProvider serviceProvider, Stream stream, IEnumerable<JsonConverter>? jsonConverters = null)
            : base(serviceProvider, stream, jsonConverters)
        {
        }

        /// <summary>
        /// Invoked remotely - <see cref="WellKnownServiceHubServices.ServiceHubServiceBase_Initialize"/>
        /// </summary>
        public virtual void Initialize(PinnedSolutionInfo info)
        {
            // set pinned solution info
            _lazyRoslynServices = null;
            _solutionInfo = info;
        }

        protected RoslynServices RoslynServices
        {
            get
            {
                // must be initialized
                Contract.ThrowIfNull(_solutionInfo);

                return _lazyRoslynServices ??= new RoslynServices(_solutionInfo.ScopeId, AssetStorage, RoslynServices.HostServices);
            }
        }

        protected Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        {
            // must be initialized
            Contract.ThrowIfNull(_solutionInfo);

            return GetSolutionAsync(RoslynServices, _solutionInfo, cancellationToken);
        }

        private static Task<Solution> GetSolutionAsync(RoslynServices roslynService, PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            var solutionController = (ISolutionController)roslynService.SolutionService;
            return solutionController.GetSolutionAsync(solutionInfo.SolutionChecksum, solutionInfo.FromPrimaryBranch, solutionInfo.WorkspaceVersion, cancellationToken);
        }
    }

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
        protected readonly AssetStorage AssetStorage;

        protected ServiceBase(IServiceProvider serviceProvider, Stream stream, IEnumerable<JsonConverter>? jsonConverters = null)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);

            // in unit test, service provider will return asset storage, otherwise, use the default one
            AssetStorage = (AssetStorage)serviceProvider.GetService(typeof(AssetStorage)) ?? AssetStorage.Default;

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Log(TraceEventType.Information, "Service instance created");

            // invoke all calls incoming over the stream on this service instance:
            EndPoint = new RemoteEndPoint(stream, Logger, incomingCallTarget: this, jsonConverters);
        }

        protected bool IsDisposed => EndPoint.IsDisposed;

        public void Dispose()
        {
            if (IsDisposed)
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

        protected SolutionService CreateSolutionService(PinnedSolutionInfo solutionInfo)
            => new SolutionService(SolutionService.CreateAssetProvider(solutionInfo, AssetStorage));

        protected Task<Solution> GetSolutionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
            => CreateSolutionService(solutionInfo).GetSolutionAsync(solutionInfo, cancellationToken);

        protected async Task<T> RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken, [CallerMemberName]string? callerName = null)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                return await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (EndPoint.ReportAndPropagateUnexpectedException(ex, cancellationToken, callerName))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected async Task RunServiceAsync(Func<Task> callAsync, CancellationToken cancellationToken, [CallerMemberName]string? callerName = null)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (EndPoint.ReportAndPropagateUnexpectedException(ex, cancellationToken, callerName))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected T RunService<T>(Func<T> call, CancellationToken cancellationToken, [CallerMemberName]string? callerName = null)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                return call();
            }
            catch (Exception ex) when (EndPoint.ReportAndPropagateUnexpectedException(ex, cancellationToken, callerName))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected void RunService(Action call, CancellationToken cancellationToken, [CallerMemberName]string? callerName = null)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                call();
            }
            catch (Exception ex) when (EndPoint.ReportAndPropagateUnexpectedException(ex, cancellationToken, callerName))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
