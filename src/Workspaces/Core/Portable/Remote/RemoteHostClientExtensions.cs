// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostClientExtensions
    {
        /// <summary>
        /// Create <see cref="RemoteHostClient.Session"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static Task<RemoteHostClient.Session> TryCreateServiceSessionAsync(this RemoteHostClient client, string serviceName, CancellationToken cancellationToken) =>
            client.TryCreateSessionAsync(serviceName, callbackTarget: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Create <see cref="SolutionAndSessionHolder"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static Task<SolutionAndSessionHolder> TryCreateServiceSessionAsync(this RemoteHostClient client, string serviceName, Solution solution, CancellationToken cancellationToken) =>
            client.TryCreateServiceSessionAsync(serviceName, solution, callbackTarget: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Create <see cref="SolutionAndSessionHolder"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static async Task<SolutionAndSessionHolder> TryCreateServiceSessionAsync(this RemoteHostClient client, string serviceName, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            var session = await client.TryCreateSessionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (session == null)
            {
                return null;
            }

            var scope = await GetPinnedScopeAsync(solution, cancellationToken).ConfigureAwait(false);
            if (scope == null)
            {
                return null;
            }

            return await SolutionAndSessionHolder.CreateAsync(session, scope, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create <see cref="KeepAliveSessionHolder"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static Task<KeepAliveSessionHolder> TryCreateServiceKeepAliveSessionAsync(this RemoteHostClient client, string serviceName, CancellationToken cancellationToken) =>
            TryCreateServiceKeepAliveSessionAsync(client, serviceName, callbackTarget: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Create <see cref="KeepAliveSessionHolder"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static async Task<KeepAliveSessionHolder> TryCreateServiceKeepAliveSessionAsync(this RemoteHostClient client, string serviceName, object callbackTarget, CancellationToken cancellationToken)
        {
            var session = await client.TryCreateSessionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (session == null)
            {
                return null;
            }

            return new KeepAliveSessionHolder(client, session, serviceName, callbackTarget, cancellationToken);
        }

        public static Task<SolutionAndSessionHolder> TryCreateCodeAnalysisServiceSessionAsync(this RemoteHostClient client, Solution solution, CancellationToken cancellationToken) =>
            TryCreateCodeAnalysisServiceSessionAsync(client, solution, callbackTarget: null, cancellationToken: cancellationToken);

        public static Task<SolutionAndSessionHolder> TryCreateCodeAnalysisServiceSessionAsync(this RemoteHostClient client, Solution solution, object callbackTarget, CancellationToken cancellationToken) =>
            client.TryCreateServiceSessionAsync(WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget, cancellationToken);

        public static Task<RemoteHostClient> TryGetRemoteHostClientAsync(this Workspace workspace, CancellationToken cancellationToken) =>
            workspace.Services.GetService<IRemoteHostClientService>()?.TryGetRemoteHostClientAsync(cancellationToken);

        public static Task<SolutionAndSessionHolder> TryCreateCodeAnalysisServiceSessionAsync(this Solution solution, Option<bool> featureOption, CancellationToken cancellationToken)
             => TryCreateCodeAnalysisServiceSessionAsync(solution, featureOption, callbackTarget: null, cancellationToken: cancellationToken);

        public static bool IsOutOfProcessEnabled(this Workspace workspace, Option<bool> featureOption)
        {
            // If the feature has explicitly opted out of OOP then we won't run it OOP.
            var outOfProcessAllowed = workspace.Options.GetOption(featureOption);
            if (!outOfProcessAllowed)
            {
                return false;
            }

            if (workspace.Options.GetOption(RemoteFeatureOptions.OutOfProcessAllowed))
            {
                // If the user has explicitly enabled OOP, then the feature is allowed to run in OOP.
                return true;
            }

            // Otherwise we check if the user is in the AB experiment enabling OOP.

            var experimentEnabled = workspace.Services.GetService<IExperimentationService>();
            if (!experimentEnabled.IsExperimentEnabled(WellKnownExperimentNames.RoslynFeatureOOP))
            {
                return false;
            }

            return true;
        }

        public static async Task<RemoteHostClient> TryGetRemoteHostClientAsync(
            this Workspace workspace, Option<bool> featureOption, CancellationToken cancellationToken)
        {
            if (!workspace.IsOutOfProcessEnabled(featureOption))
            {
                return null;
            }

            var client = await workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            return client;
        }

        public static async Task<SolutionAndSessionHolder> TryCreateCodeAnalysisServiceSessionAsync(
            this Solution solution, Option<bool> option, object callbackTarget, CancellationToken cancellationToken)
        {
            var workspace = solution.Workspace;
            var client = await TryGetRemoteHostClientAsync(workspace, option, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.TryCreateCodeAnalysisServiceSessionAsync(solution, callbackTarget, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// Synchronize given solution as primary workspace solution in remote host
        /// </summary>
        public static async Task SynchronizePrimaryWorkspaceAsync(this Workspace workspace, Solution solution, CancellationToken cancellationToken)
        {
            if (solution.BranchId != solution.Workspace.PrimaryBranchId)
            {
                return;
            }

            var remoteHostClient = await workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                return;
            }

            using (Logger.LogBlock(FunctionId.SolutionChecksumUpdater_SynchronizePrimaryWorkspace, cancellationToken))
            {
                var checksum = await solution.State.GetChecksumAsync(cancellationToken).ConfigureAwait(false);

                await remoteHostClient.RunOnRemoteHostAsync(
                    WellKnownRemoteHostServices.RemoteHostService, solution,
                    nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync), checksum, cancellationToken).ConfigureAwait(false);
            }
        }

        public static async Task<PinnedRemotableDataScope> GetPinnedScopeAsync(this Solution solution, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                return null;
            }

            var service = solution.Workspace.Services.GetService<ISolutionSynchronizationService>();
            return await service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);
        }
    }
}