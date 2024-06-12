// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Threading;
using Microsoft.Win32.SafeHandles;
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
        private readonly Process? _remoteProcess;
        private readonly SafeProcessHandle? _remoteProcessHandle;

        public BrokeredServiceConnection(
            ServiceDescriptor serviceDescriptor,
            object? callbackTarget,
            IRemoteServiceCallbackDispatcher? callbackDispatcher,
            ServiceBrokerClient serviceBrokerClient,
            SolutionAssetStorage solutionAssetStorage,
            IErrorReportingService? errorReportingService,
            IRemoteHostClientShutdownCancellationService? shutdownCancellationService,
            Process? remoteProcess)
        {
            Contract.ThrowIfFalse((callbackDispatcher == null) == (serviceDescriptor.ClientInterface == null));

            _serviceDescriptor = serviceDescriptor;
            _serviceBrokerClient = serviceBrokerClient;
            _solutionAssetStorage = solutionAssetStorage;
            _errorReportingService = errorReportingService;
            _shutdownCancellationService = shutdownCancellationService;
            _callbackDispatcher = callbackDispatcher;
            _callbackHandle = callbackDispatcher?.CreateHandle(callbackTarget) ?? default;
            _remoteProcess = remoteProcess;
            _remoteProcessHandle = _remoteProcess?.SafeHandle;
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

        public override async ValueTask<bool> TryInvokeAsync(SolutionState solution, Func<TService, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
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

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionState solution, Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
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

        public override async ValueTask<bool> TryInvokeAsync(SolutionState solution, ProjectId projectId, Func<TService, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, projectId, cancellationToken).ConfigureAwait(false);
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

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionState solution, ProjectId projectId, Func<TService, Checksum, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, projectId, cancellationToken).ConfigureAwait(false);
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

        public override async ValueTask<bool> TryInvokeAsync(SolutionState solution, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
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

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionState solution, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
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

        public override async ValueTask<bool> TryInvokeAsync(SolutionState solution, ProjectId projectId, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, projectId, cancellationToken).ConfigureAwait(false);
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

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(SolutionState solution, ProjectId projectId, Func<TService, Checksum, RemoteServiceCallbackId, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(_callbackDispatcher is not null);

            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, projectId, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                return await invocation(rental.Service, scope.SolutionChecksum, _callbackHandle.Id, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // multi-solution, no callback

        public override async ValueTask<bool> TryInvokeAsync(SolutionState solution1, SolutionState solution2, Func<TService, Checksum, Checksum, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope1 = await _solutionAssetStorage.StoreAssetsAsync(solution1, cancellationToken).ConfigureAwait(false);
                using var scope2 = await _solutionAssetStorage.StoreAssetsAsync(solution2, cancellationToken).ConfigureAwait(false);
                using var rental = await RentServiceAsync(cancellationToken).ConfigureAwait(false);
                await invocation(rental.Service, scope1.SolutionChecksum, scope2.SolutionChecksum, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (ReportUnexpectedException(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        // Exceptions

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

            try
            {
                // Process.ExitCode throws "Process was not started by this object, so requested information cannot be determined.",
                // Use Win32 API directly.
                if (_remoteProcess?.HasExited == true && NativeMethods.GetExitCodeProcess(_remoteProcessHandle!, out var exitCode))
                {
                    message += $" Exit code {exitCode}";
                }
            }
            catch
            {
            }

            _errorReportingService.ShowFeatureNotAvailableErrorInfo(message, TelemetryFeatureName.GetRemoteFeatureName(_serviceDescriptor.ComponentName, _serviceDescriptor.SimpleName), internalException);
        }
    }

    internal static partial class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetExitCodeProcess(SafeProcessHandle processHandle, out int exitCode);
    }
}
