// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Host.Mef;
using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceProjectGuidService), ServiceLayer.Default)]
    [Shared]
    internal partial class WorkspaceProjectGuidService : IWorkspaceProjectGuidService
    {
        [ImportingConstructor]
        public WorkspaceProjectGuidService()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new WorkspaceProjectGuidService();
        }

        public Guid GetProjectGuid(Project project)
        {
            return Guid.Empty;
        }
    }
}

