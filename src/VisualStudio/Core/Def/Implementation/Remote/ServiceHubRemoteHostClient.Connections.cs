﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.ServiceHub.Client;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal sealed partial class ServiceHubRemoteHostClient : RemoteHostClient
    {
        private static class Connections
        {
            /// <summary>
            /// call <paramref name="funcAsync"/> and retry up to <paramref name="timeout"/> if the call throws
            /// <typeparamref name="TException"/>. any other exception from the call won't be handled here.
            /// </summary>
            public static async Task<TResult> RetryRemoteCallAsync<TException, TResult>(
                Workspace workspace,
                Func<Task<TResult>> funcAsync,
                TimeSpan timeout,
                CancellationToken cancellationToken) where TException : Exception
            {
                const int retry_delayInMS = 50;

                using (var pooledStopwatch = SharedPools.Default<Stopwatch>().GetPooledObject())
                {
                    var watch = pooledStopwatch.Object;
                    watch.Start();

                    while (watch.Elapsed < timeout)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            return await funcAsync().ConfigureAwait(false);
                        }
                        catch (TException)
                        {
                            // throw cancellation token if operation is cancelled
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        // wait for retry_delayInMS before next try
                        await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);

                        ReportTimeout(watch);
                    }
                }

                // operation timed out, more than we are willing to wait
                ShowInfoBar(workspace);

                // user didn't ask for cancellation, but we can't fullfill this request. so we
                // create our own cancellation token and then throw it. this doesn't guarantee
                // 100% that we won't crash, but this is at least safest way we know until user
                // restart VS (with info bar)
                using (var ownCancellationSource = new CancellationTokenSource())
                {
                    ownCancellationSource.Cancel();
                    ownCancellationSource.Token.ThrowIfCancellationRequested();
                }

                throw ExceptionUtilities.Unreachable;
            }

            public static async Task<Stream> RequestServiceAsync(
                Workspace workspace,
                HubClient client,
                string serviceName,
                HostGroup hostGroup,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                const int max_retry = 10;
                const int retry_delayInMS = 50;

                RemoteInvocationException lastException = null;

                var descriptor = new ServiceDescriptor(serviceName) { HostGroup = hostGroup };

                // call to get service can fail due to this bug - devdiv#288961 or more.
                // until root cause is fixed, we decide to have retry rather than fail right away
                for (var i = 0; i < max_retry; i++)
                {
                    try
                    {
                        // we are wrapping HubClient.RequestServiceAsync since we can't control its internal timeout value ourselves.
                        // we have bug opened to track the issue.
                        // https://devdiv.visualstudio.com/DefaultCollection/DevDiv/Editor/_workitems?id=378757&fullScreen=false&_a=edit

                        // retry on cancellation token since HubClient will throw its own cancellation token
                        // when it couldn't connect to service hub service for some reasons
                        // (ex, OOP process GC blocked and not responding to request)
                        //
                        // we have double re-try here. we have these 2 seperated since 2 retries are for different problems.
                        // as noted by 2 different issues above at the start of each 2 different retries.
                        // first retry most likely deal with real issue on servicehub, second retry (cancellation) is to deal with
                        // by design servicehub behavior we don't want to use.
                        return await RetryRemoteCallAsync<OperationCanceledException, Stream>(
                            workspace,
                            () => client.RequestServiceAsync(descriptor, cancellationToken),
                            timeout,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (RemoteInvocationException ex)
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

                    // wait for retry_delayInMS before next try
                    await Task.Delay(retry_delayInMS, cancellationToken).ConfigureAwait(false);
                }

                // crash right away to get better dump. otherwise, we will get dump from async exception
                // which most likely lost all valuable data
                FatalError.ReportUnlessCanceled(lastException);
                GC.KeepAlive(lastException);

                // unreachable
                throw ExceptionUtilities.Unreachable;
            }

            #region code related to make diagnosis easier later

            private static readonly TimeSpan s_reportTimeout = TimeSpan.FromMinutes(10);
            private static bool s_timeoutReported = false;

            private static void ReportTimeout(Stopwatch watch)
            {
                // if we tried for 10 min and still couldn't connect. NFW (non fatal watson) some data
                if (!s_timeoutReported && watch.Elapsed > s_reportTimeout)
                {
                    s_timeoutReported = true;

                    // report service hub logs along with dump
                    (new Exception("RequestServiceAsync Timeout")).ReportServiceHubNFW("RequestServiceAsync Timeout");
                }
            }

            private static bool s_infoBarReported = false;

            private static void ShowInfoBar(Workspace workspace)
            {
                // use info bar to show warning to users
                if (workspace != null && !s_infoBarReported)
                {
                    // do not report it multiple times
                    s_infoBarReported = true;

                    // use info bar to show warning to users
                    workspace.Services.GetService<IErrorReportingService>()?.ShowGlobalErrorInfo(
                        ServicesVSResources.Unfortunately_a_process_used_by_Visual_Studio_has_encountered_an_unrecoverable_error_We_recommend_saving_your_work_and_then_closing_and_restarting_Visual_Studio);
                }
            }
            #endregion
        }
    }
}
