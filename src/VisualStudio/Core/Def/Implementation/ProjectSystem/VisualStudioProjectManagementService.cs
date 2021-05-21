// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ProjectManagement;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Roslyn.Utilities;

namespace Roslyn.VisualStudio.Services.Implementation.ProjectSystem
{
    [ExportWorkspaceService(typeof(IProjectManagementService), ServiceLayer.Host), Shared]
    internal class VisualStudioProjectManagementService : ForegroundThreadAffinitizedObject, IProjectManagementService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioProjectManagementService(IThreadingContext threadingContext)
            : base(threadingContext)
        {
        }

        public string GetDefaultNamespace(Microsoft.CodeAnalysis.Project project, Workspace workspace)
        {
            this.AssertIsForeground();

            if (project.Language == LanguageNames.VisualBasic)
            {
                return "";
            }

            var defaultNamespace = "";

            if (workspace is VisualStudioWorkspaceImpl vsWorkspace)
            {
                vsWorkspace.GetProjectData(project.Id,
                    out _, out var envDTEProject);

                try
                {
                    defaultNamespace = (string)envDTEProject.ProjectItems.ContainingProject.Properties.Item("DefaultNamespace").Value; // Do not Localize
                }
                catch (ArgumentException)
                {
                    // DefaultNamespace does not exist for this project.
                }
            }

            return defaultNamespace;
        }

        public IList<string> GetFolders(ProjectId projectId, Workspace workspace)
        {
            var folders = new List<string>();

            if (workspace is VisualStudioWorkspaceImpl vsWorkspace)
            {
                vsWorkspace.GetProjectData(projectId,
                    out var hierarchy, out var envDTEProject);

                var projectItems = envDTEProject.ProjectItems;

                var projectItemsStack = new Stack<Tuple<ProjectItem, string>>();

                // Populate the stack
                projectItems.OfType<ProjectItem>().Where(n => n.IsFolder()).Do(n => projectItemsStack.Push(Tuple.Create(n, "\\")));
                while (projectItemsStack.Count != 0)
                {
                    var projectItemTuple = projectItemsStack.Pop();
                    var projectItem = projectItemTuple.Item1;
                    var currentFolderPath = projectItemTuple.Item2;

                    var folderPath = currentFolderPath + projectItem.Name + "\\";

                    folders.Add(folderPath);
                    projectItem.ProjectItems.OfType<ProjectItem>().Where(n => n.IsFolder()).Do(n => projectItemsStack.Push(Tuple.Create(n, folderPath)));
                }
            }

            return folders;
        }
    }
}
