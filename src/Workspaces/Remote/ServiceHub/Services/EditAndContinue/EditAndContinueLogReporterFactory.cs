// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.BrokeredServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Creates the <see cref="IEditAndContinueLogReporter"/> workspace service in hosts that expose a service broker
/// (currently the remote/OOP host). The reporter forwards EnC trace logs to the debugger's hot reload logger.
/// </summary>
[ExportWorkspaceServiceFactory(typeof(IEditAndContinueLogReporter), ServiceLayer.Host), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class EditAndContinueLogReporterFactory(
    IAsynchronousOperationListenerProvider listenerProvider) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        var serviceBrokerProvider = workspaceServices.GetRequiredService<IServiceBrokerProvider>();
        return new EditAndContinueLogReporter(serviceBrokerProvider.ServiceBroker, listenerProvider);
    }
}
