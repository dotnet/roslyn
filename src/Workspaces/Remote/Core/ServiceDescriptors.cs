// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.TodoComments;
using Microsoft.ServiceHub.Framework;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Service descriptors of brokered ServiceHub services.
    /// </summary>
    internal static class ServiceDescriptors
    {
        public static ServiceDescriptor RemoteTodoCommentsService64 { get; } = ServiceDescriptor.CreateRemoteServiceDescriptor(
            WellKnownServiceHubService.RemoteTodoCommentsService,
            clientInterface: typeof(ITodoCommentsListener),
            isRemoteHost64Bit: true);

        public static ServiceDescriptor RemoteTodoCommentsService32 { get; } = ServiceDescriptor.CreateRemoteServiceDescriptor(
            WellKnownServiceHubService.RemoteTodoCommentsService,
            clientInterface: typeof(ITodoCommentsListener),
            isRemoteHost64Bit: false);

        public static ServiceRpcDescriptor GetServiceDescriptor(this WellKnownServiceHubService service, bool isRemoteHost64Bit)
            => (service, isRemoteHost64Bit) switch
            {
                (WellKnownServiceHubService.RemoteTodoCommentsService, true) => RemoteTodoCommentsService64,
                (WellKnownServiceHubService.RemoteTodoCommentsService, false) => RemoteTodoCommentsService32,
                _ => throw ExceptionUtilities.UnexpectedValue(service)
            };
    }
}
