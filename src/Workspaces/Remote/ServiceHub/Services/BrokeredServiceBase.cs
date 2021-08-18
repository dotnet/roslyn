// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Base type for Roslyn brokered services hosted in ServiceHub.
    /// </summary>
    internal abstract partial class BrokeredServiceBase : IDisposable
    {
        private readonly TraceSource _logger;
        protected readonly RemoteWorkspaceManager WorkspaceManager;

        protected readonly SolutionAssetSource SolutionAssetSource;
        protected readonly ServiceBrokerClient ServiceBrokerClient;

        // test data are only available when running tests:
        internal readonly RemoteHostTestData? TestData;

        static BrokeredServiceBase()
        {
        }

        protected BrokeredServiceBase(in ServiceConstructionArguments arguments)
        {
            _logger = (TraceSource)arguments.ServiceProvider.GetService(typeof(TraceSource));

            TestData = (RemoteHostTestData?)arguments.ServiceProvider.GetService(typeof(RemoteHostTestData));
            WorkspaceManager = TestData?.WorkspaceManager ?? RemoteWorkspaceManager.Default;

#pragma warning disable VSTHRD012 // Provide JoinableTaskFactory where allowed
            ServiceBrokerClient = new ServiceBrokerClient(arguments.ServiceBroker);
#pragma warning restore

            SolutionAssetSource = new SolutionAssetSource(ServiceBrokerClient);
        }

        public void Dispose()
            => ServiceBrokerClient.Dispose();

        public RemoteWorkspace GetWorkspace()
            => WorkspaceManager.GetWorkspace();

        protected void Log(TraceEventType errorType, string message)
            => _logger.TraceEvent(errorType, 0, $"{GetType()}: {message}");

        protected ValueTask<Solution> GetSolutionAsync(PinnedSolutionInfo solutionInfo, CancellationToken cancellationToken)
        {
            var workspace = GetWorkspace();
            var assetProvider = workspace.CreateAssetProvider(solutionInfo, WorkspaceManager.SolutionAssetCache, SolutionAssetSource);
            return workspace.GetSolutionAsync(assetProvider, solutionInfo.SolutionChecksum, solutionInfo.FromPrimaryBranch, solutionInfo.WorkspaceVersion, cancellationToken);
        }

        protected ValueTask<T> RunServiceAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();
            return RunServiceImplAsync(implementation, cancellationToken);
        }

        internal static async ValueTask<T> RunServiceImplAsync<T>(Func<CancellationToken, ValueTask<T>> implementation, CancellationToken cancellationToken)
        {
            try
            {
                return await implementation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected ValueTask RunServiceAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
        {
            WorkspaceManager.SolutionAssetCache.UpdateLastActivityTime();
            return RunServiceImplAsync(implementation, cancellationToken);
        }

        internal static async ValueTask RunServiceImplAsync(Func<CancellationToken, ValueTask> implementation, CancellationToken cancellationToken)
        {
            try
            {
                await implementation(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (FatalError.ReportAndPropagateUnlessCanceled(ex, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }
    }
}
