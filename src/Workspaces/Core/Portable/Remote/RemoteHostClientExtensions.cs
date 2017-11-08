// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
            var session = await client.TryCreateConnectionAsync(serviceName, callbackTarget, cancellationToken).ConfigureAwait(false);
            if (session == null)
            {
                return null;
            }

            var scope = await GetPinnedScopeAsync(solution, cancellationToken).ConfigureAwait(false);
            return await SessionWithSolution.CreateAsync(session, scope, cancellationToken).ConfigureAwait(false);
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

                try
                {
                    // this will return false only if OOP doesn't exist. 
                    // otherwise, it will throw and handled as usual. here we don't care about return value
                    // since if OOP doesn't exist, this call is NOOP and don't have any meaning.
                    await remoteHostClient.TryRunRemoteAsync(
                        WellKnownRemoteHostServices.RemoteHostService, solution,
                        nameof(IRemoteHostService.SynchronizePrimaryWorkspaceAsync), checksum, cancellationToken).ConfigureAwait(false);
                }
                catch (UnexpectedRemoteHostException)
                {
                    // ignore unexpected remote host exception. it is allowed here since it is part of OOP engine.
                    // no one outside of engine should ever catch this exception or care about it.
                    // we catch here so that we don't physically crash VS and give users time to save and exist VS
                }
            }
        }

        public static async Task<PinnedRemotableDataScope> GetPinnedScopeAsync(this Solution solution, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);

            var service = solution.Workspace.Services.GetService<IRemotableDataService>();
            return await service.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);
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

        /// <summary>
        /// this is a workaround to not crash host when remote call is failed for a reason not
        /// related to us. example will be extension manager failure, connection creation failure
        /// and etc. this is a special exception that should be only used in very specific cases.
        /// 
        /// no one except code related to OOP engine should care about this exception. 
        /// if this is fired, then VS is practicially in corrupted/crashed mode. we just didn't
        /// physically crashed VS due to feedbacks that want to give users time to save their works.
        /// when this is fired, VS clearly shows users to save works and restart VS since VS is crashed.
        /// 
        /// so no one should ever, outside of OOP engine, try to catch this exception and try to recover.
        /// 
        /// that facts this inherits cancellation exception is an implementation detail to make VS not physically crash.
        /// it doesn't mean one should try to recover from it or treat it as cancellation exception.
        /// 
        /// we choose cancellation exception since we didn't want this workaround to be too intrusive.
        /// on our code. we already handle cancellation gracefully and recover properly in most of cases.
        /// but that doesn't mean we want to let users to keep use VS. like I stated above, once this is
        /// fired, VS is logically crashed. we just want VS to be stable enough until users save and exist VS.
        /// 
        /// this is a workaround since we would like to go back to normal crash behavior
        /// if enough of the above issues are fixed or we implements official NFW framework in Roslyn
        /// </summary>
        public class UnexpectedRemoteHostException : OperationCanceledException
        {
            public UnexpectedRemoteHostException() :
                base("unexpected remote host exception", CancellationToken.None)
            {
            }
        }
    }
}
