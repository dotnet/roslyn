// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using Microsoft.VisualStudio.LanguageServices;
using System;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceProjectGuidService), ServiceLayer.Host)]
    [Shared]
    internal partial class VisualStudioWorkspaceProjectGuidServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioWorkspaceProjectGuidServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new VisualStudioWorkspaceProjectGuidService();
        }

        private class VisualStudioWorkspaceProjectGuidService : IWorkspaceProjectGuidService
        {
            public Guid GetProjectGuid(Project project)
            {
                if (project.Solution.Workspace is VisualStudioWorkspace vsWorkspace)
                {
                    return vsWorkspace.GetProjectGuid(project.Id);
                }
                return Guid.Empty;
            }
        }
    }
}

