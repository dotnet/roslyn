// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;
using StreamJsonRpc;
using StreamJsonRpc.Protocol;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class BrokeredServiceConnection<TService> : RemoteServiceConnection<TService>
        where TService : class
    {
        private readonly struct Rental : IDisposable
        {
#pragma warning disable ISB002 // Avoid storing rentals in fields
            private readonly ServiceBrokerClient.Rental<TService> _proxyRental;
#pragma warning restore

            public readonly TService Service;

            public Rental(ServiceBrokerClient.Rental<TService> proxyRental, TService service)
            {
                _proxyRental = proxyRental;
                Service = service;
            }

            public void Dispose()
                => _proxyRental.Dispose();
        }

        private readonly IErrorReportingService? _errorReportingService;
        private readonly IRemoteHostClientShutdownCancellationService? _shutdownCancellationService;
        private readonly SolutionAssetStorage _solutionAssetStorage;

        private readonly ServiceDescriptor _serviceDescriptor;
        private readonly ServiceBrokerClient _serviceBrokerClient;
        private readonly RemoteServiceCallbackDispatcher.Handle _callbackHandle;
        private readonly IRemoteServiceCallbackDispatcher? _callbackDispatcher;

        public BrokeredServiceConnection(
            ServiceDescriptor serviceDescriptor,
            object? callbackTarget,
            IRemoteServiceCallbackDispatcher? callbackDispatcher,
            ServiceBrokerClient serviceBrokerClient,
            SolutionAssetStorage solutionAssetStorage,
            IErrorReportingService? errorReportingService,
            IRemoteHostClientShutdownCancellationService? shutdownCancellationService)
        {
            Contract.ThrowIfFalse((callbackDispatcher == null) == (serviceDescriptor.ClientInterface == null));

            _serviceDescriptor = serviceDescriptor;
            _serviceBrokerClient = serviceBrokerClient;
            _solutionAssetStorage = solutionAssetStorage;
            _errorReportingService = errorReportingService;
            _shutdownCancellationService = shutdownCancellationService;
            _callbackDispatcher = callbackDispatcher;
            _callbackHandle = callbackDispatcher?.CreateHandle(callbackTarget) ?? default;
        }

        public override void Dispose()
        {
            _callbackHandle.Dispose();
        }

        private async ValueTask<Rental> RentServiceAsync(CancellationToken cancellationToken)
        {
            // Make sure we are on the thread pool to avoid UI thread dependencies if external code uses ConfigureAwait(true)
            await TaskScheduler.Default;

            var options = new ServiceActivationOptions
            {
                ClientRpcTarget = _callbackDispatcher
            };

            var proxyRental = await _serviceBrokerClient.GetProxyAsync<TService>(_serviceDescriptor, options, cancellationToken).ConfigureAwait(false);
            var service = proxyRental.Proxy;
            Contract.ThrowIfNull(service);
            return new Rental(proxyRental, service);
        }

        // no solution, no callback

        public override async ValueTask<bool> TryInvokeAsync(Func<TService, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<TService, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // no solution, callback

        public override async ValueTask<bool> TryInvokeAsync(Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<TService, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // solution, no callback

        public override async ValueTask<bool> TryInvokeAsync(Solution solution, Func<TService, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, scope.SolutionChecksum, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Solution solution, Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, scope.SolutionChecksum, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // project, no callback

        public override async ValueTask<bool> TryInvokeAsync(Project project, Func<TService, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(project, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, scope.SolutionChecksum, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Project project, Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(project, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, scope.SolutionChecksum, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // solution, callback

        public override async ValueTask<bool> TryInvokeAsync(Solution solution, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, scope.SolutionChecksum, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Solution solution, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, scope.SolutionChecksum, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // project, callback

        public override async ValueTask<bool> TryInvokeAsync(Project project, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(project, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, scope.SolutionChecksum, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Project project, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(project, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, scope.SolutionChecksum, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // streaming

        /// <param name="service">The service instance.</param>
        /// <param name="invocation">A callback to asynchronously write data. The callback is required to complete the
        /// <see cref="PipeWriter"/> except in cases where the callback throws an exception.</param>
        /// <param name="reader">A callback to asynchronously read data. The callback is allowed, but not required, to
        /// complete the <see cref="PipeReader"/>.</param>
        /// <param name="cancellationToken">A cancellation token the operation will observe.</param>
        internal static async ValueTask<TResult> InvokeStreamingServiceAsync<TResult>(
            TService service,
            Func<TService, PipeWriter, CancellationToken, ValueTask> invocation,
            Func<PipeReader, CancellationToken, ValueTask<TResult>> reader,
            CancellationToken cancellationToken)
        {
            // We can cancel at entry, but once the pipe operations are scheduled we rely on both operations running to
            // avoid deadlocks (the exception handler in 'writerTask' ensures progress is made in 'readerTask').
            cancellationToken.ThrowIfCancellationRequested();
            var mustNotCancelToken = CancellationToken.None;

            // After this point, the full cancellation sequence is as follows:
            //  1. 'cancellationToken' indicates cancellation is requested
            //  2. 'invocation' and 'readerTask' have cancellation requested
            //  3. 'invocation' stops writing to 'pipe.Writer'
            //  4. 'pipe.Writer' is completed
            //  5. 'readerTask' continues reading until EndOfStreamException (workaround for https://github.com/AArnott/Nerdbank.Streams/issues/361)
            //  6. 'pipe.Reader' is completed
            //  7. OperationCanceledException is thrown back to the caller

            var pipe = new Pipe();

            // Create new tasks that both start executing, rather than invoking the delegates directly
            // to make sure both invocation and reader start executing and transfering data.

            var writerTask = Task.Run(async () =>
            {
                try
                {
                    await invocation(service, pipe.Writer, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // Ensure that the writer is complete if an exception is thrown
                    // before the writer is passed to the RPC proxy. Once it's passed to the proxy 
                    // the proxy should complete it as soon as the remote side completes it.
                    await pipe.Writer.CompleteAsync(e).ConfigureAwait(false);

                    throw;
                }
            }, mustNotCancelToken);

            var readerTask = Task.Run(
                async () =>
                {
                    Exception? exception = null;

                    try
                    {
                        return await reader(pipe.Reader, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception e) when ((exception = e) == null)
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                    finally
                    {
                        await pipe.Reader.CompleteAsync(exception).ConfigureAwait(false);
                    }
                }, mustNotCancelToken);

            await Task.WhenAll(writerTask, readerTask).ConfigureAwait(false);

            return readerTask.Result;
        }

        private bool ReportUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException)
            {
                // It's a bug for a service to throw OCE based on a different cancellation token than it has received in the call.
                // The server side filter will report NFW in such scenario, so that the underlying issue can be fixed.
                // Do not treat this as a critical failure of the service for now and only fail in debug build.
                Debug.Assert(cancellationToken.IsCancellationRequested);

                return false;
            }

            // Do not report telemetry when the host is shutting down or the remote service threw an IO exception:
            if (IsHostShuttingDown || IsRemoteIOException(exception))
            {
                return true;
            }

            return FatalError.ReportAndCatch(exception);
        }

        private bool IsHostShuttingDown
            => _shutdownCancellationService?.ShutdownToken.IsCancellationRequested == true;

        // TODO: we need https://github.com/microsoft/vs-streamjsonrpc/issues/468 to be implemented in order to check for IOException subtypes.
        private static bool IsRemoteIOException(Exception exception)
            => exception is RemoteInvocationException { ErrorData: CommonErrorData { TypeName: "System.IO.IOException" } };

        private void OnUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            // If the cancellation token passed to the remote call is not linked with the host shutdown cancellation token,
            // various non-cancellation exceptions may occur during the remote call.
            // Throw cancellation exception if the cancellation token is signaled.
            // If it is not then show info to the user that the service is not available dure to shutdown.
            cancellationToken.ThrowIfCancellationRequested();

            if (_errorReportingService == null)
            {
                return;
            }

            // Show the error on the client. See https://github.com/dotnet/roslyn/issues/40476 for error classification details.
            // Based on the exception type and the state of the system we report one of the following:
            // - "Feature xyz is currently unavailable due to an intermittent error. Please try again later. Error message: '{1}'" (RemoteInvocationException: IOException)
            // - "Feature xyz is currently unavailable due to an internal error [Details]" (exception is RemoteInvocationException, MessagePackSerializationException, ConnectionLostException)
            // - "Feature xyz is currently unavailable since Visual Studio is shutting down" (connection exceptions during shutdown cancellation when cancellationToken is not signalled)

            // We expect all RPC calls to complete and not drop the connection.
            // ConnectionLostException indicates a bug that is likely thrown because the remote process crashed.
            // Currently, ConnectionLostException is also throw when the result of the RPC method fails to serialize 
            // (see https://github.com/microsoft/vs-streamjsonrpc/issues/549)

            string message;
            Exception? internalException = null;
            var featureName = _serviceDescriptor.GetFeatureDisplayName();

            if (IsRemoteIOException(exception))
            {
                message = string.Format(RemoteWorkspacesResources.Feature_0_is_currently_unavailable_due_to_an_intermittent_error, featureName, exception.Message);
            }
            else if (IsHostShuttingDown)
            {
                message = string.Format(RemoteWorkspacesResources.Feature_0_is_currently_unavailable_host_shutting_down, featureName, _errorReportingService.HostDisplayName);
            }
            else
            {
                message = string.Format(RemoteWorkspacesResources.Feature_0_is_currently_unavailable_due_to_an_internal_error, featureName);
                internalException = exception;
            }

            _errorReportingService.ShowFeatureNotAvailableErrorInfo(message, TelemetryFeatureName.GetRemoteFeatureName(_serviceDescriptor.ComponentName, _serviceDescriptor.SimpleName), internalException);
        }
    }
}
