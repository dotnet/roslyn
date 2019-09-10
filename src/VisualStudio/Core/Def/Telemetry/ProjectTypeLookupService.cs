// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportWorkspaceService(typeof(IProjectTypeLookupService), ServiceLayer.Host), Shared]
    internal class ProjectTypeLookupService : IProjectTypeLookupService
    {
        [ImportingConstructor]
        public ProjectTypeLookupService()
        {
        }

        public string GetProjectType(Workspace workspace, ProjectId projectId)
        {
            if (!(workspace is VisualStudioWorkspace vsWorkspace) || projectId == null)
            {
                return string.Empty;
            }

            if (!(vsWorkspace.GetHierarchy(projectId) is IVsAggregatableProject aggregatableProject))
            {
                return string.Empty;
            }

            if (ErrorHandler.Succeeded(aggregatableProject.GetAggregateProjectTypeGuids(out var projectType)))
            {
                return projectType;
            }

            return projectType ?? string.Empty;
        }
    }
}
