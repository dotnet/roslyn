// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using EnvDTE80;

namespace Templates.Editorconfig.Wizard
{
    internal static class ExtensionMethods
    {
        private const string SolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        
        public static (bool success, string rootFolder) TryGetRootFolder(this Project project, string solutionFullName)
        {
            if (project == null)
            {
                return (false, null);
            }

            if (project.IsKind(SolutionFolder))
            {
                return (true, Path.GetDirectoryName(solutionFullName));
            }

            if (string.IsNullOrEmpty(project.FullName))
            {
                return (false, null);
            }

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                return File.Exists(project.FullName)
                    ? (true, Path.GetDirectoryName(project.FullName)) 
                    : (false, null);
            }

            if (Directory.Exists(fullPath))
            {
                return (true, fullPath);
            }

            if (File.Exists(fullPath))
            {
                return (true, Path.GetDirectoryName(fullPath));
            }

            return (false, null);
        }

        public static (bool success, ProjectItem projectItem) TryAddFileToSolution(this Solution2 solution, DTE2 dte, string fileName)
        {
            Project currentProject = null;
            foreach (Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.Name == "Solution Items")
                {
                    currentProject = project;
                    break;
                }
            }

            if (currentProject == null)
            {
                currentProject = solution.AddSolutionFolder("Solution Items");
            }

            return currentProject.TryAddFileToProject(dte, fileName, "None");
        }

        public static (bool success, ProjectItem projectItem) TryAddFileToProject(this Project project, DTE2 dte, string file, string itemType = null)
        {
            if (IsKind(project, ProjectTypes.ASPNET_5, ProjectTypes.SSDT))
            {
                return (true, dte.Solution.FindProjectItem(file));
            }

            var (success, rootFolder) = project.TryGetRootFolder(dte.Solution.FullName);
            if (!success)
            {
                return (false, null);
            }

            if (string.IsNullOrEmpty(rootFolder) || !file.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                return (false, null);
            }

            ProjectItem item = project.ProjectItems.AddFromFile(file);
            item.SetItemType(itemType);
            return (true, item);
        }

        public static void SetItemType(this ProjectItem item, string itemType)
        {
            try
            {
                if (item == null || item.ContainingProject == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(itemType) || IsKind(item.ContainingProject, ProjectTypes.WEBSITE_PROJECT, ProjectTypes.UNIVERSAL_APP))
                {
                    return;
                }

                item.Properties.Item("ItemType").Value = itemType;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
            }
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (string guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
