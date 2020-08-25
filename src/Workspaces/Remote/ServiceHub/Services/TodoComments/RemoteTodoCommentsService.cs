// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class RemoteTodoCommentsService : ServiceBase, IRemoteTodoCommentsService
    {
        internal sealed class Factory : IServiceHubServiceFactory
        {
            public Task<object> CreateAsync(
                Stream stream,
                IServiceProvider hostProvidedServices,
                ServiceActivationOptions serviceActivationOptions,
                IServiceBroker serviceBroker,
                AuthorizationServiceClient? authorizationServiceClient)
            {
                // Dispose the AuthorizationServiceClient since we won't be using it
                authorizationServiceClient?.Dispose();

                return Task.FromResult<object>(new RemoteTodoCommentsService(stream, hostProvidedServices, serviceBroker));
            }
        }

        private readonly ITodoCommentsListener _callback;

        public RemoteTodoCommentsService(Stream stream, IServiceProvider serviceProvider, IServiceBroker serviceBroker)
            : base(serviceProvider, serviceBroker)
        {
            var descriptor = (IntPtr.Size == 4) ? ServiceDescriptors.RemoteTodoCommentsService32 : ServiceDescriptors.RemoteTodoCommentsService64;
            _callback = descriptor.ConstructRpc<ITodoCommentsListener>(rpcTarget: this, stream.UsePipe());
        }

        public Task ComputeTodoCommentsAsync(CancellationToken cancellation)
        {
            return RunServiceAsync(() =>
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

                return Task.CompletedTask;
            }, cancellation);
        }
    }
}
