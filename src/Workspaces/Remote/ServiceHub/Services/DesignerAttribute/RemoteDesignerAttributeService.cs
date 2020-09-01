// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DesignerAttribute;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteDesignerAttributeService : ServiceBase, IRemoteDesignerAttributeService
    {
        internal sealed class Factory : FactoryBase
        {
            internal override WellKnownServiceHubService ServiceId
                => WellKnownServiceHubService.RemoteTodoCommentsService;

            internal override object CreateService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, ServiceActivationOptions serviceActivationOptions)
                => new RemoteDesignerAttributeService(serviceProvider, serviceBroker, (IDesignerAttributeListener)serviceActivationOptions.ClientRpcTarget!);
        }

        private readonly IDesignerAttributeListener _callback;

        public RemoteDesignerAttributeService(IServiceProvider serviceProvider, IServiceBroker serviceBroker, IDesignerAttributeListener callback)
            : base(serviceProvider, serviceBroker)
        {
            _callback = callback;
        }

        public Task StartScanningForDesignerAttributesAsync(CancellationToken cancellation)
        {
            return RunServiceAsync(() =>
            {
                var registrationService = GetWorkspace().Services.GetRequiredService<ISolutionCrawlerRegistrationService>();
                var analyzerProvider = new RemoteDesignerAttributeIncrementalAnalyzerProvider(_callback);

                registrationService.AddAnalyzerProvider(
                    analyzerProvider,
                    new IncrementalAnalyzerProviderMetadata(
                        nameof(RemoteDesignerAttributeIncrementalAnalyzerProvider),
                        highPriorityForActiveFile: false,
                        workspaceKinds: WorkspaceKind.RemoteWorkspace));

                return Task.CompletedTask;
            }, cancellation);
        }
    }
}
