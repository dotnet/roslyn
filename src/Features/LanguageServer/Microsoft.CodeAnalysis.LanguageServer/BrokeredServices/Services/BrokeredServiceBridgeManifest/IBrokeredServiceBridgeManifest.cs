// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Utilities.ServiceBroker;

namespace Microsoft.CodeAnalysis.LanguageServer.BrokeredServices.Services.BrokeredServiceBridgeManifest;

/// <summary>
/// Defines a service to be used by the remote party to determine which services from this container
/// it should add to its container.  This is useful in the case where the remote party connects to other processes
/// that proffer the same services as we do (e.g. intrinsic services).
/// Both are proffered as <see cref="ServiceSource.OtherProcessOnSameMachine"/> and therefore conflict.
/// </summary>
internal interface IBrokeredServiceBridgeManifest
{
    /// <summary>
    /// Returns services that the container wishes to expose across the bridge.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask<IReadOnlyCollection<ServiceMoniker>> GetAvailableServicesAsync(CancellationToken cancellationToken);
}

