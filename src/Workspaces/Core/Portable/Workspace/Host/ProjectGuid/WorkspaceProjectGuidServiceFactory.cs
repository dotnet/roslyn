// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Host.Mef;
using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceProjectGuidService), ServiceLayer.Default)]
    [Shared]
    internal partial class WorkspaceProjectGuidServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public WorkspaceProjectGuidServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new WorkspaceProjectGuidService();
        }

        private class WorkspaceProjectGuidService : IWorkspaceProjectGuidService
        {
            public Guid GetProjectGuid(Project project)
            {
                return Guid.Empty;
            }
        }
    }
}

