// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices;

/// <summary>
/// Allow services to export IOnServiceBrokerInitialized and getting called back when service broker is initialized
/// </summary>
internal interface IOnServiceBrokerInitialized
{
    void OnServiceBrokerInitialized(IServiceBroker serviceBroker);
}
