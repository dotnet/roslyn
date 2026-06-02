// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// Workspace service that can be used to fetch a service broker instance from a workspace.
/// </summary>
[ExportWorkspaceService(typeof(IServiceBrokerProvider), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class ServiceBrokerProvider() : IServiceBrokerProvider
{
    private readonly TaskCompletionSource<IBrokeredServiceContainer> _serviceBrokerContainerTask = new();

    /// <summary>
    /// Returns an instance of <see cref="IServiceBroker"/> that will wait for the service broker to be available before invoking the requested method.
    /// </summary>
    /// <remarks>
    /// Each call to this property returns a new instance of <see cref="IServiceBroker"/> from <see cref="IBrokeredServiceContainer.GetFullAccessServiceBroker"/>.
    /// This is observable to callers in a few ways, including that they only get the <see cref="IServiceBroker.AvailabilityChanged"/> events based on their own service queries.
    /// </remarks>
    public IServiceBroker ServiceBroker
    {
        get
        {
            return new WrappedServiceBroker(_serviceBrokerContainerTask.Task);
        }
    }

    public void SetContainer(IBrokeredServiceContainer container)
    {
        Contract.ThrowIfTrue(_serviceBrokerContainerTask.Task.IsCompleted);
        _serviceBrokerContainerTask.SetResult(container);
    }
}
