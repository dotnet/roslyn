// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Options;

namespace Microsoft.CodeAnalysis.Execution
{
    [ExportWorkspaceServiceFactory(typeof(IExecutionHostingService)), Shared]
    internal class ExecutionHostingServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new ExecutionHostingService((MefWorkspaceServices)workspaceServices);
        }

        private class ExecutionHostingService : IExecutionHostingService
        {
            private readonly MefWorkspaceServices _workspaceServices;

            // accumulated cache for host specific services
            private ImmutableDictionary<string, MefHostSpecificServices> _hostSpecificServicesMap
                = ImmutableDictionary<string, MefHostSpecificServices>.Empty;

            public ExecutionHostingService(MefWorkspaceServices workspaceServices)
            {
                _workspaceServices = workspaceServices;
            }

            public HostSpecificServices GetService()
            {
                // REVIEW: should each service determine what is the best service with current context or
                //         engine should determine for them.

                // TODO: here we need to put some heuristic to determine what is best host to run
                // the service. for now, just explicit options
                if (_workspaceServices.Workspace.Options.GetOption(RuntimeOptions.RemoteHostAvailable))
                {
                    return GetService(HostKinds.OutOfProc);
                }

                return GetService(HostKinds.InProc);
            }

            public HostSpecificServices GetService(string host)
            {
                var currentServicesMap = _hostSpecificServicesMap;

                MefHostSpecificServices hostSpecificServices;
                if (!currentServicesMap.TryGetValue(host, out hostSpecificServices))
                {
                    hostSpecificServices = ImmutableInterlocked.GetOrAdd(ref _hostSpecificServicesMap, host, h => new MefHostSpecificServices(_workspaceServices, h));
                }

                return hostSpecificServices;
            }
        }
    }
}
