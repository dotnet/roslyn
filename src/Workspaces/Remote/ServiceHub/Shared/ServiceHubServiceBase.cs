// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    // TODO: all service hub service should be extract to interface so that it can support multiple hosts.
    //       right now, tightly coupled to service hub
    internal abstract class ServiceHubServiceBase : IDisposable
    {
        private static int s_instanceId;

        private readonly CancellationTokenSource _cancellationTokenSource;

        protected readonly int InstanceId;

        protected readonly JsonRpc Rpc;
        protected readonly TraceSource Logger;
        protected readonly AssetStorage AssetStorage;
        protected readonly CancellationToken CancellationToken;

        /// <summary>
        /// Session Id of this service. caller and callee share this id which one
        /// can use to find matching caller and callee when debugging or logging
        /// </summary>
        private int _sessionId;

        /// <summary>
        /// Mark whether the solution checksum it got is for primary branch or not 
        /// 
        /// this flag will be passed down to solution controller to help
        /// solution service's cache policy. for more detail, see <see cref="SolutionService"/>
        /// </summary>
        private bool _fromPrimaryBranch;

        /// <summary>
        /// solution this connection belong to
        /// </summary>
        private Checksum _solutionChecksumOpt;

        private RoslynServices _lazyRoslynServices;

        [Obsolete("For backward compatibility. this will be removed once all callers moved to new ctor")]
        protected ServiceHubServiceBase(Stream stream, IServiceProvider serviceProvider)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);

            // in unit test, service provider will return asset storage, otherwise, use the default one
            AssetStorage = (AssetStorage)serviceProvider.GetService(typeof(AssetStorage)) ?? AssetStorage.Default;

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Logger.TraceInformation($"{DebugInstanceString} Service instance created");

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;

            Rpc = JsonRpc.Attach(stream, this);
            Rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);

            Rpc.Disconnected += OnRpcDisconnected;
        }

        protected ServiceHubServiceBase(IServiceProvider serviceProvider, Stream stream)
        {
            InstanceId = Interlocked.Add(ref s_instanceId, 1);

            // in unit test, service provider will return asset storage, otherwise, use the default one
            AssetStorage = (AssetStorage)serviceProvider.GetService(typeof(AssetStorage)) ?? AssetStorage.Default;

            Logger = (TraceSource)serviceProvider.GetService(typeof(TraceSource));
            Logger.TraceInformation($"{DebugInstanceString} Service instance created");

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken = _cancellationTokenSource.Token;

            // due to this issue - https://github.com/dotnet/roslyn/issues/16900#issuecomment-277378950
            // all sub type must explicitly start JsonRpc once everything is
            // setup
            Rpc = new JsonRpc(new JsonRpcMessageHandler(stream, stream), this);
            Rpc.JsonSerializer.Converters.Add(AggregateJsonConverter.Instance);
            Rpc.Disconnected += OnRpcDisconnected;
        }

        protected string DebugInstanceString => $"{GetType()} ({InstanceId})";

        protected RoslynServices RoslynServices
        {
            get
            {
                if (_lazyRoslynServices == null)
                {
                    _lazyRoslynServices = new RoslynServices(_sessionId, AssetStorage);
                }

                return _lazyRoslynServices;
            }
        }

        protected Task<Solution> GetSolutionAsync()
        {
            Contract.ThrowIfNull(_solutionChecksumOpt);

            var solutionController = (ISolutionController)RoslynServices.SolutionService;
            return solutionController.GetSolutionAsync(_solutionChecksumOpt, _fromPrimaryBranch, CancellationToken);
        }

        protected virtual void Dispose(bool disposing)
        {
            // do nothing here
        }

        protected void LogError(string message)
        {
            Log(TraceEventType.Error, message);
        }

        public virtual void Initialize(int sessionId, bool fromPrimaryBranch, Checksum solutionChecksum)
        {
            // set session related information
            _sessionId = sessionId;
            _fromPrimaryBranch = fromPrimaryBranch;

            if (solutionChecksum != null)
            {
                _solutionChecksumOpt = solutionChecksum;
            }
        }

        public void Dispose()
        {
            Rpc.Dispose();
            Dispose(false);

            Logger.TraceInformation($"{DebugInstanceString} Service instance disposed");
        }

        protected virtual void OnDisconnected(JsonRpcDisconnectedEventArgs e)
        {
            // do nothing
        }

        protected void Log(TraceEventType errorType, string message)
        {
            Logger.TraceEvent(errorType, 0, $"{DebugInstanceString} : " + message);
        }

        private void OnRpcDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
        {
            // raise cancellation
            _cancellationTokenSource.Cancel();

            OnDisconnected(e);

            if (e.Reason != DisconnectedReason.Disposed)
            {
                // this is common for us since we close connection forcefully when operation
                // is cancelled. use Warning level so that by default, it doesn't write out to
                // servicehub\log files. one can still make this to write logs by opting in.
                Log(TraceEventType.Warning, $"Client stream disconnected unexpectedly: {e.Exception?.GetType().Name} {e.Exception?.Message}");
            }
        }
    }
}