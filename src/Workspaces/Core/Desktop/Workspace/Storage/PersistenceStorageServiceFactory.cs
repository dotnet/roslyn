// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Storage
{
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Desktop), Shared]
    internal class PersistenceStorageServiceFactory : IWorkspaceServiceFactory
    {
        private IPersistentStorageService _singleton;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                var optionService = workspaceServices.GetService<IOptionService>();
                Interlocked.CompareExchange(
                    ref _singleton, 
                    new PersistentStorageService(optionService), null);
            }

            return _singleton;
        }
    }
}