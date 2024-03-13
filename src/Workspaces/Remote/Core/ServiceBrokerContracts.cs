// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.ServiceHub.Framework;

namespace Microsoft.CodeAnalysis.BrokeredServices;

internal static class ServiceBrokerContracts
{
    /// <summary>
    /// The MEF contract name for the <see cref="IServiceBroker"/> which is typically imported using MEF v1 attributes
    /// with the configuration <c>[Import(typeof(SVsFullAccessServiceBroker)]</c>. This can be imported using MEF v2
    /// using the configuration <c>[Import(<see cref="ServiceBrokerContracts"/>.<see cref="SVsFullAccessServiceBroker">SVsFullAccessServiceBroker</see>)]</c>.
    /// </summary>
    /// <seealso cref="Microsoft.VisualStudio.Shell.ServiceBroker.SVsFullAccessServiceBroker"/>
    public const string SVsFullAccessServiceBroker = "Microsoft.VisualStudio.Shell.ServiceBroker.SVsFullAccessServiceBroker";
}
