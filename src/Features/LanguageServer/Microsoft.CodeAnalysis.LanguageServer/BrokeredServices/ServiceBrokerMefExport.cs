// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices;

/// <summary>
/// Exports an <see cref="IServiceBroker"/> for convenient and potentially cross-IDE importing by other features.
/// </summary>
/// <remarks>
/// Each import site gets its own <see cref="IServiceBroker"/> instance to match the behavior of calling <see cref="IBrokeredServiceContainer.GetFullAccessServiceBroker"/>
/// which returns a private instance for everyone.
/// This is observable to callers in a few ways, including that they only get the <see cref="IServiceBroker.AvailabilityChanged"/> events
/// based on their own service queries.
/// MEF will dispose of each instance as its lifetime comes to an end.
/// </remarks>
#pragma warning disable RS0030 // This is intentionally using System.ComponentModel.Composition for compatibility with MEF service broker.
[Export]
internal class ServiceBrokerMefExport
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public ServiceBrokerMefExport()
    {
    }

    [Export(typeof(SVsFullAccessServiceBroker))]
    public IServiceBroker ServiceBroker => this.Container?.GetFullAccessServiceBroker() ?? throw new InvalidOperationException($"Set {nameof(this.Container)} first.");

    [Export("PrivateBrokeredServiceContainer")]
    internal BrokeredServiceContainer? Container { get; set; }
}
#pragma warning restore RS0030 // Do not used banned APIs
