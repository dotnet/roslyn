// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.TableDataSource
{
    internal abstract class TableItem
    {
        public readonly Workspace Workspace;

        private string _lazyProjectName;

        // Guid.Empty if the item is aggregated, or the item doesn't have an associated project.
        public readonly Guid ProjectGuid;

        // Empty for non-aggregated items:
        public readonly string[] ProjectNames;
        public readonly Guid[] ProjectGuids;

        public TableItem(Workspace workspace, string projectName, Guid projectGuid, string[] projectNames, Guid[] projectGuids)
        {
            Contract.ThrowIfNull(workspace);
            Contract.ThrowIfNull(projectNames);
            Contract.ThrowIfNull(projectGuids);

            Workspace = workspace;
            _lazyProjectName = projectName;
            ProjectGuid = projectGuid;
            ProjectNames = projectNames;
            ProjectGuids = projectGuids;
        }

        internal static void GetProjectNameAndGuid(Workspace workspace, ProjectId projectId, out string projectName, out Guid projectGuid)
        {
            projectName = (projectId == null) ? null : workspace.CurrentSolution.GetProject(projectId)?.Name ?? ServicesVSResources.Unknown2;
            projectGuid = (projectId != null && workspace is VisualStudioWorkspace vsWorkspace) ? vsWorkspace.GetProjectGuid(projectId) : Guid.Empty;
        }

        public abstract TableItem WithAggregatedData(string[] projectNames, Guid[] projectGuids);

        public abstract DocumentId DocumentId { get; }
        public abstract ProjectId ProjectId { get; }

        public abstract LinePosition GetOriginalPosition();
        public abstract string GetOriginalFilePath();
        public abstract bool EqualsIgnoringLocation(TableItem other);

        public string ProjectName
        {
            get
            {
                if (_lazyProjectName != null)
                {
                    return _lazyProjectName;
                }

                if (ProjectNames.Length > 0)
                {
                    return _lazyProjectName = string.Join(", ", ProjectNames);
                }

                return null;
            }
        }

    }
}
