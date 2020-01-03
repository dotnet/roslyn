// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal partial class InteractiveHost
    {
        private sealed class LazyRemoteService
        {
            public readonly AsyncLazy<InitializedRemoteService> InitializedService;
            public readonly CancellationTokenSource CancellationSource;
            public readonly InteractiveHostOptions Options;
            public readonly InteractiveHost Host;
            public readonly bool SkipInitialization;
            public readonly int InstanceId;

            public LazyRemoteService(InteractiveHost host, InteractiveHostOptions options, int instanceId, bool skipInitialization)
            {
                InitializedService = new AsyncLazy<InitializedRemoteService>(TryStartAndInitializeProcessAsync, cacheResult: true);
                CancellationSource = new CancellationTokenSource();
                InstanceId = instanceId;
                Options = options;
                Host = host;
                SkipInitialization = skipInitialization;
            }

            public void Dispose()
            {
                // Cancel the creation of the process if it is in progress.
                // If it is the cancellation will clean up all resources allocated during the creation.
                CancellationSource.Cancel();

                // If the value has been calculated already, dispose the service.
                if (InitializedService.TryGetValue(out var initializedService))
                {
                    initializedService.ServiceOpt?.Dispose();
                }
            }

            private async Task<InitializedRemoteService> TryStartAndInitializeProcessAsync(CancellationToken cancellationToken)
            {
                try
                {
                    Host.ProcessStarting?.Invoke(Options.InitializationFile != null);

                    var remoteService = await TryStartProcessAsync(Options.GetHostPath(), Options.Culture, cancellationToken).ConfigureAwait(false);
                    if (remoteService == null)
                    {
                        return default;
                    }

                    if (SkipInitialization)
                    {
                        return new InitializedRemoteService(remoteService, new RemoteExecutionResult(success: true));
                    }

                    bool initializing = true;
                    cancellationToken.Register(() =>
                    {
                        if (initializing)
                        {
                            // kill the process without triggering auto-reset:
                            remoteService.Dispose();
                        }
                    });

                    // try to execute initialization script:
                    var initializationResult = await Async<RemoteExecutionResult>(remoteService, (service, operation) =>
                    {
                        service.InitializeContextAsync(operation, Options.InitializationFile, isRestarting: InstanceId > 1);
                    }).ConfigureAwait(false);

                    initializing = false;

                    if (!initializationResult.Success)
                    {
                        Host.ReportProcessExited(remoteService.Process);
                        remoteService.Dispose();

                        return default;
                    }

                    // Hook up a handler that initiates restart when the process exits.
                    // Note that this is just so that we restart the process as soon as we see it dying and it doesn't need to be 100% bullet-proof.
                    // If we don't receive the "process exited" event we will restart the process upon the next remote operation.
                    remoteService.HookAutoRestartEvent();

                    return new InitializedRemoteService(remoteService, initializationResult);
                }
                catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private Task<RemoteService> TryStartProcessAsync(string hostPath, CultureInfo culture, CancellationToken cancellationToken)
            {
                return Task.Run(() => Host.TryStartProcess(hostPath, culture, cancellationToken));
            }
        }
    }
}
