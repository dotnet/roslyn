﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TodoComments;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTodoCommentsDiscoveryService : BrokeredServiceBase, IRemoteTodoCommentsDiscoveryService
    {
        internal sealed class Factory : FactoryBase<IRemoteTodoCommentsDiscoveryService, ITodoCommentsListener>
        {
            protected override IRemoteTodoCommentsDiscoveryService CreateService(in ServiceConstructionArguments arguments, RemoteCallback<ITodoCommentsListener> callback)
                => new RemoteTodoCommentsDiscoveryService(arguments, callback);
        }

        private readonly RemoteCallback<ITodoCommentsListener> _callback;

        public RemoteTodoCommentsDiscoveryService(in ServiceConstructionArguments arguments, RemoteCallback<ITodoCommentsListener> callback)
            : base(arguments)
        {
            _callback = callback;
        }

        public ValueTask ComputeTodoCommentsAsync(CancellationToken cancellationToken)
        {
            return RunServiceAsync(cancellationToken =>
            {
                var workspace = GetWorkspace();
                var registrationService = workspace.Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteTodoCommentsIncrementalAnalyzerProvider(_callback);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteTodoCommentsIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return default;
            }, cancellationToken);
        }
    }
}
