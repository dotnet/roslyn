// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// Allows MEF components to participate in brokered service container initialization.
/// Implementations can register <see cref="ServicesToRegister"/> and proffer <see cref="Proffer"/> services into the container,
/// and/or respond to the service broker being fully initialized via <see cref="OnServiceBrokerInitialized"/>.
/// </summary>
internal interface IServiceBrokerInitializer
{
    /// <summary>
    /// Gets the set of services that this initializer will proffer into the service container via <see cref="Proffer(GlobalBrokeredServiceContainer)"/>
    /// </summary>
    ImmutableDictionary<ServiceMoniker, ServiceRegistration> ServicesToRegister { get; }

    /// <summary>
    /// Proffers services into the container.  Services must be registered via <see cref="ServicesToRegister"/> to be proffered here.
    /// </summary>
    void Proffer(GlobalBrokeredServiceContainer container);

    /// <summary>
    /// Called when the service broker has been fully initialized.
    /// </summary>
    void OnServiceBrokerInitialized(IServiceBroker serviceBroker, CancellationToken cancellationToken);
}

