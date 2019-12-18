// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        internal static class Connections
        {
            private static readonly TimeSpan s_reportTimeout = TimeSpan.FromMinutes(10);
            private static bool s_timeoutReported = false;

            private static async Task<Stream> RequestServiceWithCancellationRetryAsync(
                Workspace workspace,
                HubClient client,
                ServiceDescriptor descriptor,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                var retryDelay = TimeSpan.FromMilliseconds(50);

                using (var pooledStopwatch = SharedPools.Default<Stopwatch>().GetPooledObject())
                {
                    var watch = pooledStopwatch.Object;
                    watch.Start();

                    while (watch.Elapsed < timeout)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            return await client.RequestServiceAsync(descriptor, cancellationToken).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                        {
                            // Retry on cancellation that is not sourced by our cancellation token.
                            // Since HubClient will throw when it can't connect to service hub service (e.g. timeout, disposal).
                        }

                        // wait before next try
                        await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);

                        // if we tried for too long and still couldn't connect, report non-fatal Watson
                        if (!s_timeoutReported && watch.Elapsed > s_reportTimeout)
                        {
                            s_timeoutReported = true;

                            // report service hub logs along with dump
                            new Exception("RequestServiceAsync Timeout").ReportServiceHubNFW("RequestServiceAsync Timeout");
                        }
                    }
                }

                // operation timed out, more than we are willing to wait
                RemoteHostCrashInfoBar.ShowInfoBar(workspace);

                // throw soft crash exception to minimize hard crash. it doesn't
                // guarantee 100% hard crash free. but 99% it doesn't cause
                // hard crash
                throw new SoftCrashException("retry timed out", cancellationToken);
            }

            public static async Task<Stream> RequestServiceAsync(
                Workspace workspace,
                HubClient client,
                string serviceName,
                HostGroup hostGroup,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                const int MaxRetryAttempts = 10;
                var retryDelay = TimeSpan.FromMilliseconds(50);

                Exception lastException = null;

                var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };

                // Call to get service can fail due to this bug - devdiv#288961 or more.
                // until root cause is fixed, we decide to have retry rather than fail right away
                //
                // We have double re-try here. We have these 2 separated since 2 retries are for different problems.
                // First retry most likely deal with real issue on ServiceHub, second retry (cancellation) is to deal with
                // ServiceHub behavior we don't want to use.
                for (var i = 0; i < MaxRetryAttempts; i++)
                {
                    try
                    {
                        return await RequestServiceWithCancellationRetryAsync(
                            workspace,
                            client,
                            descriptor,
                            timeout,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!(ex is OperationCanceledException))
                    {
                        // save info only if it failed with different issue than before.
                        if (lastException?.Message != ex.Message)
                        {
                            // RequestServiceAsync should never fail unless service itself is actually broken.
                            // So far, we catched multiple issues from this NFW. so we will keep this NFW.
                            ex.ReportServiceHubNFW("RequestServiceAsync Failed");

                            lastException = ex;
                        }
                    }

                    // wait before next try
                    await Task.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                }

                RemoteHostCrashInfoBar.ShowInfoBar(workspace, lastException);

                // raise soft crash exception rather than doing hard crash.
                // we had enough feedback from users not to crash VS on servicehub failure
                throw new SoftCrashException("RequestServiceAsync Failed", lastException, cancellationToken);
            }
        }
    }
}
