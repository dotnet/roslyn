// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class BrokeredServiceConnection<TService> : RemoteServiceConnection<TService>
        where TService : class
    {
        private readonly IErrorReportingService _errorReportingService;
        private readonly SolutionAssetStorage _solutionAssetStorage;
        private readonly TService _service;

        public BrokeredServiceConnection(TService service, SolutionAssetStorage solutionAssetStorage, IErrorReportingService errorReportingService)
        {
            _errorReportingService = errorReportingService;
            _solutionAssetStorage = solutionAssetStorage;
            _service = service;
        }

        public override void Dispose()
            => (_service as IDisposable)?.Dispose();

        // without solution

        public override async ValueTask<bool> TryInvokeAsync(Func<TService, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                await invocation(_service, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Func<TService, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                return await invocation(_service, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TArgs, TResult>(Func<TService, TArgs, CancellationToken, ValueTask<TResult>> invocation, TArgs args, CancellationToken cancellationToken)
        {
            try
            {
                return await invocation(_service, args, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        // with solution

        public override async ValueTask<bool> TryInvokeAsync(Solution solution, Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                await invocation(_service, scope.SolutionInfo, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception exception) when (FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return false;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TResult>(Solution solution, Func<TService, PinnedSolutionInfo, CancellationToken, ValueTask<TResult>> invocation, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                return await invocation(_service, scope.SolutionInfo, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        public override async ValueTask<Optional<TResult>> TryInvokeAsync<TArgs, TResult>(Solution solution, Func<TService, PinnedSolutionInfo, TArgs, CancellationToken, ValueTask<TResult>> invocation, TArgs args, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = await _solutionAssetStorage.StoreAssetsAsync(solution, cancellationToken).ConfigureAwait(false);
                return await invocation(_service, scope.SolutionInfo, args, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (FatalError.ReportWithoutCrashUnlessCanceled(exception, cancellationToken))
            {
                OnUnexpectedException(exception, cancellationToken);
                return default;
            }
        }

        private void OnUnexpectedException(Exception exception, CancellationToken cancellationToken)
        {
            // Remote call may fail with different exception even when our cancellation token is signaled
            // (e.g. on shutdown if the connection is dropped):
            cancellationToken.ThrowIfCancellationRequested();

            // TODO: better message depending on the exception (https://github.com/dotnet/roslyn/issues/40476):
            // "Feature xyz is currently unavailable due to network issues" (connection exceptions)
            // "Feature xyz is currently unavailable due to an internal error [Details]" (exception is RemoteInvocationException, serialization issues)
            // "Feature xyz is currently unavailable" (connection exceptions during shutdown cancellation when cancellationToken is not signalled)

            _errorReportingService?.ShowRemoteHostCrashedErrorInfo(exception);
        }
    }
}
