// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    // REVIEW: should there be auto retry mechanism such as if out of proc run fails, it automatically calls inProc one?
    internal static class Extensions
    {
        public static THostSpecificService GetHostSpecificServiceAvailable<THostSpecificService>(this HostWorkspaceServices hostServices) where THostSpecificService : IHostSpecificService
        {
            // first get best host specific services from current context and see whether specific service exist in the services pack
            // if it doesn't, move down from outofproc to inproc to find service requested.
            var service = GetHostSpecificServices(hostServices).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.OutOfProc).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.InProc).GetService<THostSpecificService>() as IHostSpecificService;

            return (THostSpecificService)service;
        }

        public static THostSpecificService GetHostSpecificServiceAvailable<THostSpecificService>(this HostWorkspaceServices hostServices, string host) where THostSpecificService : IHostSpecificService
        {
            // first get host specific services from given host and see whether specific service exist in the services pack
            // if it doesn't, move down from outofproc to inproc to find service requested.
            var service = GetHostSpecificServices(hostServices, host).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.OutOfProc).GetService<THostSpecificService>() as IHostSpecificService ??
                          GetHostSpecificServices(hostServices, HostKinds.InProc).GetService<THostSpecificService>() as IHostSpecificService;

            return (THostSpecificService)service;
        }

        private static THostSpecificService GetHostSpecificService<THostSpecificService>(this HostWorkspaceServices hostServices, string host) where THostSpecificService : IHostSpecificService
        {
            return GetHostSpecificServices(hostServices, host).GetService<THostSpecificService>();
        }

        private static HostSpecificServices GetHostSpecificServices(this HostWorkspaceServices hostServices)
        {
            return hostServices.GetRequiredService<IExecutionHostingService>().GetService();
        }

        private static HostSpecificServices GetHostSpecificServices(this HostWorkspaceServices hostServices, string host)
        {
            return hostServices.GetRequiredService<IExecutionHostingService>().GetService(host);
        }
    }
}
