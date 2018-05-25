// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostClientExtensions
    {
        /// <summary>
        /// Create <see cref="RemoteHostClient.Connection"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating connection could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static Task<RemoteHostClient.Connection> TryCreateConnectionAsync(
            this RemoteHostClient client, string serviceName, CancellationToken cancellationToken)
            => client.TryCreateConnectionAsync(serviceName, callbackTarget: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Create <see cref="SessionWithSolution"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static Task<SessionWithSolution> TryCreateSessionAsync(
            this RemoteHostClient client, string serviceName, Solution solution, CancellationToken cancellationToken)
            => client.TryCreateSessionAsync(serviceName, solution, callbackTarget: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Create <see cref="SessionWithSolution"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static async Task<SessionWithSolution> TryCreateSessionAsync(
            this RemoteHostClient client, string serviceName, Solution solution, object callbackTarget, CancellationToken cancellationToken)
        {
            var connection = await client.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return null;
            }

            return await SessionWithSolution.CreateAsync(connection, solution, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Create <see cref="KeepAliveSession"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static Task<KeepAliveSession> TryCreateKeepAliveSessionAsync(
            this RemoteHostClient client, string serviceName, CancellationToken cancellationToken)
            => TryCreateKeepAliveSessionAsync(client, serviceName, callbackTarget: null, cancellationToken: cancellationToken);

        /// <summary>
        /// Create <see cref="KeepAliveSession"/> for the <paramref name="serviceName"/> if possible.
        /// otherwise, return null.
        /// 
        /// Creating session could fail if remote host is not available. one of example will be user killing
        /// remote host.
        /// </summary>
        public static async Task<KeepAliveSession> TryCreateKeepAliveSessionAsync(
            this RemoteHostClient client, string serviceName, object callbackTarget, CancellationToken cancellationToken)
        {
            var connection = await client.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return null;
            }

            return new KeepAliveSession(client, connection, serviceName, callbackTarget);
        }

        public static Task<SessionWithSolution> TryCreateCodeAnalysisSessionAsync(
            this RemoteHostClient client, Solution solution, CancellationToken cancellationToken)
            => TryCreateCodeAnalysisSessionAsync(client, solution, callbackTarget: null, cancellationToken: cancellationToken);

        public static Task<SessionWithSolution> TryCreateCodeAnalysisSessionAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, CancellationToken cancellationToken)
            => client.TryCreateSessionAsync(WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget, cancellationToken);

        public static Task<KeepAliveSession> TryCreateCodeAnalysisKeepAliveSessionAsync(
            this RemoteHostClient client, CancellationToken cancellationToken)
            => TryCreateCodeAnalysisKeepAliveSessionAsync(client, callbackTarget: null, cancellationToken: cancellationToken);

        public static Task<KeepAliveSession> TryCreateCodeAnalysisKeepAliveSessionAsync(
            this RemoteHostClient client, object callbackTarget, CancellationToken cancellationToken)
            => client.TryCreateKeepAliveSessionAsync(WellKnownServiceHubServices.CodeAnalysisService, callbackTarget, cancellationToken);

        public static Task<RemoteHostClient> TryGetRemoteHostClientAsync(
            this Workspace workspace, CancellationToken cancellationToken)
            => workspace.Services.GetService<IRemoteHostClientService>()?.TryGetRemoteHostClientAsync(cancellationToken);

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

        public static Task<RemoteHostClient> TryGetRemoteHostClientAsync(
            this Workspace workspace, Option<bool> featureOption, CancellationToken cancellationToken)
        {
            if (!workspace.IsOutOfProcessEnabled(featureOption))
            {
                return SpecializedTasks.Default<RemoteHostClient>();
            }

            return workspace.TryGetRemoteHostClientAsync(cancellationToken);
        }

        public static Task<bool> TryRunRemoteAsync(
            this RemoteHostClient client, string serviceName, Solution solution, string targetName, object argument, CancellationToken cancellationToken)
            => TryRunRemoteAsync(client, serviceName, solution, targetName, new object[] { argument }, cancellationToken);

        public static Task<bool> TryRunRemoteAsync(
            this RemoteHostClient client, string serviceName, Solution solution, string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            object callbackTarget = null;
            return TryRunRemoteAsync(client, serviceName, solution, callbackTarget, targetName, arguments, cancellationToken);
        }

        public static async Task<bool> TryRunRemoteAsync(
            this RemoteHostClient client, string serviceName, Solution solution, object callbackTarget,
            string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (var session = await client.TryCreateSessionAsync(serviceName, solution, callbackTarget, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    // can't create Session. RemoteHost seems not responding for some reasons such as OOP gone.
                    return false;
                }

                await session.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        public static async Task<bool> TryRunRemoteAsync(
            this RemoteHostClient client, string serviceName, string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (var connection = await client.TryCreateConnectionAsync(serviceName, cancellationToken).ConfigureAwait(false))
            {
                if (connection == null)
                {
                    // can't create Connection. RemoteHost seems not responding for some reasons such as OOP gone.
                    return false;
                }

                await connection.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static async Task<T> TryRunRemoteAsync<T>(
            this RemoteHostClient client, string serviceName, Solution solution, string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (var session = await client.TryCreateSessionAsync(serviceName, solution, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    // can't create Session. RemoteHost seems not responding for some reasons such as OOP gone.
                    return default;
                }

                return await session.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<bool> TryRunCodeAnalysisRemoteAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, string targetName, object argument, CancellationToken cancellationToken)
            => TryRunCodeAnalysisRemoteAsync(client, solution, callbackTarget, targetName, new object[] { argument }, cancellationToken);

        public static Task<bool> TryRunCodeAnalysisRemoteAsync(
            this RemoteHostClient client, Solution solution, object callbackTarget, string targetName, object[] arguments, CancellationToken cancellationToken)
            => TryRunRemoteAsync(client, WellKnownServiceHubServices.CodeAnalysisService, solution, callbackTarget, targetName, arguments, cancellationToken);

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static Task<T> TryRunCodeAnalysisRemoteAsync<T>(
            this RemoteHostClient client, Solution solution, string targetName, object argument, CancellationToken cancellationToken)
            => TryRunCodeAnalysisRemoteAsync<T>(client, solution, targetName, new object[] { argument }, cancellationToken);

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static Task<T> TryRunCodeAnalysisRemoteAsync<T>(
            this RemoteHostClient client, Solution solution, string targetName, object[] arguments, CancellationToken cancellationToken)
            => TryRunRemoteAsync<T>(client, WellKnownServiceHubServices.CodeAnalysisService, solution, targetName, arguments, cancellationToken);

        public static Task<bool> TryRunCodeAnalysisRemoteAsync(
            this RemoteHostClient client, string targetName, object argument, CancellationToken cancellationToken)
            => TryRunRemoteAsync(client, WellKnownServiceHubServices.CodeAnalysisService, targetName, new object[] { argument }, cancellationToken);

        public static Task<bool> TryRunCodeAnalysisRemoteAsync(
            this RemoteHostClient client, string targetName, object[] arguments, CancellationToken cancellationToken)
            => TryRunRemoteAsync(client, WellKnownServiceHubServices.CodeAnalysisService, targetName, arguments, cancellationToken);

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

                await remoteHostClient.TryRunRemoteAsync(
                    WellKnownRemoteHostServices.RemoteHostService, solution,
                    nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync), checksum, cancellationToken).ConfigureAwait(false);
            }
        }

        public static Task<PinnedRemotableDataScope> GetPinnedScopeAsync(this Solution solution, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);

            var service = solution.Workspace.Services.GetService<IRemotableDataService>();
            return service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken);
        }

        public static Task<SessionWithSolution> TryCreateCodeAnalysisSessionAsync(
            this Solution solution, Option<bool> featureOption, CancellationToken cancellationToken)
            => TryCreateCodeAnalysisSessionAsync(solution, featureOption, callbackTarget: null, cancellationToken: cancellationToken);

        public static async Task<SessionWithSolution> TryCreateCodeAnalysisSessionAsync(
            this Solution solution, Option<bool> option, object callbackTarget, CancellationToken cancellationToken)
        {
            var workspace = solution.Workspace;
            var client = await TryGetRemoteHostClientAsync(workspace, option, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.TryCreateCodeAnalysisSessionAsync(solution, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        public static Task<bool> TryRunCodeAnalysisRemoteAsync(
            this Solution solution, Option<bool> option, object callbackTarget, string targetName, object argument, CancellationToken cancellationToken)
            => TryRunCodeAnalysisRemoteAsync(solution, option, callbackTarget, targetName, new object[] { argument }, cancellationToken);

        public static async Task<bool> TryRunCodeAnalysisRemoteAsync(
            this Solution solution, Option<bool> option, object callbackTarget, string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (var session = await TryCreateCodeAnalysisSessionAsync(solution, option, callbackTarget, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return false;
                }

                await session.InvokeAsync(targetName, arguments, cancellationToken).ConfigureAwait(false);
                return true;
            }
        }

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static Task<T> TryRunCodeAnalysisRemoteAsync<T>(
            this Solution solution, Option<bool> option, string targetName, object[] arguments, CancellationToken cancellationToken)
        {
            object callbackTarget = null;
            return TryRunCodeAnalysisRemoteAsync<T>(solution, option, callbackTarget, targetName, arguments, cancellationToken);
        }

        /// <summary>
        /// Run given service on remote host. if it fails to run on remote host, it will return default(T)
        /// </summary>
        public static async Task<T> TryRunCodeAnalysisRemoteAsync<T>(
            this Solution solution, Option<bool> option, object callbackTarget, string targetName, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            using (var session = await TryCreateCodeAnalysisSessionAsync(solution, option, callbackTarget, cancellationToken).ConfigureAwait(false))
            {
                if (session == null)
                {
                    return default;
                }

                return await session.InvokeAsync<T>(targetName, arguments, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
