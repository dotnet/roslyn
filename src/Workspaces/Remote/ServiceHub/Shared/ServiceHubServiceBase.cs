// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    internal abstract class ServiceHubServiceBase : IDisposable
    {
        private static int s_instanceId;

        private readonly JsonRpc _rpc;

        protected readonly int InstanceId;

        protected readonly TraceSource Logger;
        protected readonly AssetStorage AssetStorage;

        [Obsolete("don't use RPC directly but use it through StartService and InvokeAsync", error: true)]
        protected readonly JsonRpc Rpc;

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
        private PinnedSolutionInfo _solutionInfo;

        private RoslynServices _lazyRoslynServices;

        private bool _disposed;

        protected ServiceHubServiceBase(IServiceProvider serviceProvider, Stream stream)
            : this(serviceProvider, stream, SpecializedCollections.EmptyEnumerable<JsonConverter>())
        {
        }

        protected ServiceHubServiceBase(IServiceProvider serviceProvider, Stream stream, IEnumerable<JsonConverter> jsonConverters)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);
            _disposed = false;

            // in unit test, service provider will return asset storage, otherwise, use the default one
            AssetStorage = (AssetStorage)serviceProvider.GetService(typeof(AssetStorage)) ?? AssetStorage.Default;

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Logger.TraceInformation($"{DebugInstanceString} Service instance created");

            // due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // all sub type must explicitly start JsonRpc once everything is
            // setup. 
            // we also wires given json converters when creating JsonRpc so that razor or SBD can register
            // their own converter when they create their own service
            _rpc = stream.CreateStreamJsonRpc(target: this, Logger, jsonConverters);
            _rpc.Disconnected += OnRpcDisconnected;

            // we do this since we want to mark Rpc as obsolete but want to set its value for
            // partner teams until they move. we can't use Rpc directly since we will get
            // obsolete error and we can't suppress it since it is an error
            this.GetType().GetField("Rpc", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(this, _rpc);
        }

        protected event EventHandler Disconnected;

        protected string DebugInstanceString => $"{GetType()} ({InstanceId})";

        protected RoslynServices RoslynServices
        {
            get
            {
                if (_lazyRoslynServices == null)
                {
                    _lazyRoslynServices = new RoslynServices(_solutionInfo.ScopeId, AssetStorage, RoslynServices.HostServices);
                }

                return _lazyRoslynServices;
            }
        }

        protected bool IsDisposed => ((IDisposableObservable)_rpc).IsDisposed;

        protected void StartService()
        {
            _rpc.StartListening();
        }

        protected Task<TResult> InvokeAsync<TResult>(string targetName, CancellationToken cancellationToken)
        {
            return InvokeAsync<TResult>(targetName, SpecializedCollections.EmptyReadOnlyList<object>(), cancellationToken);
        }

        protected Task<TResult> InvokeAsync<TResult>(
            string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            return _rpc.InvokeWithCancellationAsync<TResult>(targetName, arguments?.AsArray(), cancellationToken);
        }

        protected Task<TResult> InvokeAsync<TResult>(
           string targetName, IReadOnlyList<object> arguments,
           Func<Stream, CancellationToken, TResult> funcWithDirectStream, CancellationToken cancellationToken)
        {
            return Extensions.InvokeAsync(_rpc, targetName, arguments, funcWithDirectStream, cancellationToken);
        }

        protected Task InvokeAsync(string targetName, CancellationToken cancellationToken)
        {
            return InvokeAsync(targetName, arguments: null, cancellationToken);
        }

        protected Task InvokeAsync(
            string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            return _rpc.InvokeWithCancellationAsync(targetName, arguments?.AsArray(), cancellationToken);
        }

        protected Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(_solutionInfo);

            return GetSolutionAsync(RoslynServices, _solutionInfo, cancellationToken);
        }

        protected Task<Solution> GetSolutionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            var localRoslynService = new RoslynServices(solutionInfo.ScopeId, AssetStorage, RoslynServices.HostServices);
            return GetSolutionAsync(localRoslynService, solutionInfo, cancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            // do nothing here
        }

        protected void LogError(string message)
        {
            Log(TraceEventType.Error, message);
        }

        public virtual void Initialize(PinnedSolutionInfo info)
        {
            // set pinned solution info
            _lazyRoslynServices = null;
            _solutionInfo = info;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                // guard us from double disposing. this can happen in unit test
                // due to how we create test mock service hub stream that tied to
                // remote host service
                return;
            }

            _disposed = true;
            _rpc.Dispose();

            Dispose(disposing: true);

            Logger.TraceInformation($"{DebugInstanceString} Service instance disposed");
        }

        protected void Log(TraceEventType errorType, string message)
        {
            Logger.TraceEvent(errorType, 0, $"{DebugInstanceString} : " + message);
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);

            // either service naturally went away since nobody is using 
            // or the other side closed the connection such as closing VS
            if (e.Reason != DisconnectedReason.LocallyDisposed &&
                e.Reason != DisconnectedReason.RemotePartyTerminated)
            {
                // we no longer close connection forcefully. so connection shouldn't go away 
                // in normal situation. if it happens, log why it did in more detail.
                LogError($@"Client stream disconnected unexpectedly: 
{nameof(e.Description)}: {e.Description}
{nameof(e.Reason)}: {e.Reason}
{nameof(e.LastMessage)}: {e.LastMessage}
{nameof(e.Exception)}: {e.Exception?.ToString()}");
            }
        }

        private static Task<Solution> GetSolutionAsync(RoslynServices roslynService, PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            var solutionController = (ISolutionController)roslynService.SolutionService;
            return solutionController.GetSolutionAsync(solutionInfo.SolutionChecksum, solutionInfo.FromPrimaryBranch, solutionInfo.WorkspaceVersion, cancellationToken);
        }

        protected async Task<T> RunServiceAsync<T>(Func<Task<T>> callAsync, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                return await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected async Task RunServiceAsync(Func<Task> callAsync, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                await callAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected T RunService<T>(Func<T> call, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                return call();
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected void RunService(Action call, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                call();
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        [Obsolete("Use one with no CancellationToken given. underlying issue has been addressed", error: true)]
        protected async Task<T> RunServiceAsync<T>(Func<CancellationToken, Task<T>> callAsync, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                return await callAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        [Obsolete("Use one with no CancellationToken given. underlying issue has been addressed", error: true)]
        protected async Task RunServiceAsync(Func<CancellationToken, Task> callAsync, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                await callAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        [Obsolete("Use one with no CancellationToken given. underlying issue has been addressed", error: true)]
        protected T RunService<T>(Func<CancellationToken, T> call, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                return call(cancellationToken);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        [Obsolete("Use one with no CancellationToken given. underlying issue has been addressed", error: true)]
        protected void RunService(Action<CancellationToken> call, CancellationToken cancellationToken)
        {
            AssetStorage.UpdateLastActivityTime();

            try
            {
                call(cancellationToken);
            }
            catch (Exception ex) when (LogUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private bool LogUnlessCanceled(Exception ex, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                LogException(ex);
            }

            return false;
        }

        protected void LogException(Exception ex)
        {
            LogError("Exception: " + ex.ToString());

            LogExtraInformation(ex);

            var callStack = new StackTrace().ToString();
            LogError("From: " + callStack);
        }

        private void LogExtraInformation(Exception ex)
        {
            if (ex == null)
            {
                return;
            }

            if (ex is ReflectionTypeLoadException reflection)
            {
                foreach (var loaderException in reflection.LoaderExceptions)
                {
                    LogError("LoaderException: " + loaderException.ToString());
                    LogExtraInformation(loaderException);
                }
            }

            if (ex is FileNotFoundException file)
            {
                LogError("FusionLog: " + file.FusionLog);
            }

            if (ex is AggregateException agg)
            {
                foreach (var innerException in agg.InnerExceptions)
                {
                    LogExtraInformation(innerException);
                }
            }
            else
            {
                LogExtraInformation(ex.InnerException);
            }
        }
    }
}
