﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostClientExtensions
    {
        [Obsolete]
        public static Task<RemoteHostClient.Session> CreateCodeAnalysisServiceSessionAsync(
            this RemoteHostClient client, Solution solution, CancellationToken cancellationToken)
        {
            return CreateCodeAnalysisServiceSessionAsync(
                client, solution, callbackTarget: null, cancellationToken: cancellationToken);
        }

        [Obsolete]
        public static Task<RemoteHostClient.Session> CreateCodeAnalysisServiceSessionAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            return TryCreateCodeAnalysisServiceSessionAsync(client, solution, callbackTarget, cancellationToken);
        }

        public static Task<RemoteHostClient.Session> TryCreateCodeAnalysisServiceSessionAsync(
            this RemoteHostClient client, Solution solution, CancellationToken cancellationToken)
        {
            return TryCreateCodeAnalysisServiceSessionAsync(
                client, solution, callbackTarget: null, cancellationToken: cancellationToken);
        }

        public static Task<RemoteHostClient.Session> TryCreateCodeAnalysisServiceSessionAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            return client.TryCreateServiceSessionAsync(
                WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget, cancellationToken);
        }

        public static Task<RemoteHostClient> TryGetRemoteHostClientAsync(this Workspace workspace, CancellationToken cancellationToken)
        {
            var clientService = workspace.Services.GetService<IRemoteHostClientService>();
            return clientService?.TryGetRemoteHostClientAsync(cancellationToken);
        }

        public static Task RunOnRemoteHostAsync(
            this RemoteHostClient client, string serviceName, Solution solution, string targetName, object argument, CancellationToken cancellationToken)
        {
            return RunOnRemoteHostAsync(client, serviceName, solution, targetName, new object[] { argument }, cancellationToken);
        }

        public static Task RunOnRemoteHostAsync(
            this RemoteHostClient client, string serviceName, Solution solution, string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            object callbackTarget = null;
            return RunOnRemoteHostAsync(client, serviceName, solution, callbackTarget, targetName, arguments, cancellationToken);
        }

        public static async Task RunOnRemoteHostAsync(
            this RemoteHostClient client, string serviceName, Solution solution, object callbackTarget,
            string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            using (var session = await client.TryCreateServiceSessionAsync(serviceName, solution, callbackTarget, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    // can't create Session. RemoteHost seems not responding for some reasons such as OOP gone.
                    return;
                }

                await session.InvokeAsync(targetName, arguments).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static async Task<T> RunOnRemoteHostAsync<T>(
            this RemoteHostClient client, string serviceName, Solution solution, string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            using (var session = await client.TryCreateServiceSessionAsync(serviceName, solution, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    // can't create Session. RemoteHost seems not responding for some reasons such as OOP gone.
                    return default(T);
                }

                return await session.InvokeAsync<T>(targetName, arguments).ConfigureAwait(false);
            }
        }

        public static Task RunCodeAnalysisServiceOnRemoteHostAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, string targetName, object argument, CancellationToken cancellationToken)
        {
            return RunCodeAnalysisServiceOnRemoteHostAsync(client, solution, callbackTarget, targetName, new object[] { argument }, cancellationToken);
        }

        public static Task RunCodeAnalysisServiceOnRemoteHostAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return RunOnRemoteHostAsync(client, WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget, targetName, arguments, cancellationToken);
        }

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static Task<T> RunCodeAnalysisServiceOnRemoteHostAsync<T>(
            this RemoteHostClient client, Solution solution, string targetName, object argument, CancellationToken cancellationToken)
        {
            return RunCodeAnalysisServiceOnRemoteHostAsync<T>(client, solution, targetName, new object[] { argument }, cancellationToken);
        }

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static Task<T> RunCodeAnalysisServiceOnRemoteHostAsync<T>(
            this RemoteHostClient client, Solution solution, string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            return RunOnRemoteHostAsync<T>(client, WellKnownServiceHubServices.CodeAnalysisService, solution, targetName, arguments, cancellationToken);
        }
    }
}