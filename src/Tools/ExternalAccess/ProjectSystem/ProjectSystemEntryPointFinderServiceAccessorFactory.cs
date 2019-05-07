// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.ProjectSystem
{
    [ExportWorkspaceServiceFactory(typeof(IProjectSystemEntryPointFinderServiceAccessor))]
    [Shared]
    internal sealed class ProjectSystemEntryPointFinderServiceAccessorFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ProjectSystemEntryPointFinderServiceAccessorFactory()
        {
        }

        [Obsolete(MefConstruction.FactoryMethodMessage, error: true)]
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new ProjectSystemEntryPointFinderServiceAccessor(workspaceServices);
        }
    }
}
