// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using EnvDTE80;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Linq;
using VSLangProj;
using Constants = EnvDTE.Constants;
using Project = EnvDTE.Project;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Utilities;

public static class VSHelpers
{
    private const string SolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

    public static DTE2 DTE { get; } = GetService<DTE, DTE2>();

    public static TReturnType GetService<TServiceType, TReturnType>()
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        return (TReturnType)ServiceProvider.GlobalProvider.GetService(typeof(TServiceType));
    }

    public static bool IsDotnet()
    {
        return DTE.Solution.Projects.OfType<Project>().Any(p => p.IsKind(PrjKind.prjKindCSharpProject, PrjKind.prjKindVBProject));
    }

    public static bool IsDotnet(string directory)
    {
        return HasCSharpFiles(directory) || HasVisualBasicFiles(directory);
    }

    public static string? GetLanguageFromDirectory(string directory)
    {
        if (HasCSharpFiles(directory))
        {
            return LanguageNames.CSharp;
        }

        if (HasVisualBasicFiles(directory))
        {
            return LanguageNames.VisualBasic;
        }

        return null;
    }

    public static bool HasCSharpFiles(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories).Any();
    }

    public static bool HasVisualBasicFiles(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.vb", SearchOption.AllDirectories).Any();
    }

    public static bool HasCSharpProjects()
    {
        return DTE.Solution.Projects.OfType<Project>().Any(p => p.IsKind(PrjKind.prjKindCSharpProject));
    }

    public static bool HasVisualBasicProjects()
    {
        return DTE.Solution.Projects.OfType<Project>().Any(p => p.IsKind(PrjKind.prjKindVBProject));
    }

    public static (bool isSolutionLevel, string? path, string? language, object? selectedItem) TryGetSelectedItemLanguageAndPath()
    {
        var items = (Array)DTE.ToolWindows.SolutionExplorer.SelectedItems;
        object? selectedItem = null;

        foreach (UIHierarchyItem selItem in items)
        {
            selectedItem = selItem.Object;

            // Check if selected item is the solution 
            if (selectedItem is EnvDTE.Solution)
            {
                return (true, Path.GetDirectoryName(DTE.Solution.FullName), HasVisualBasicProjects() ? LanguageNames.VisualBasic : LanguageNames.CSharp, selectedItem);
            }

            var containingProject = GetVSProject(selectedItem);
            if (containingProject is null)
            {
                return (false, null, null, selectedItem);
            }
            var language = containingProject.GetProjectLanguageName();

            if (selItem.Object is ProjectItem item && item.Properties != null)
            {
                if (item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a folder; add the .editorconfig file to the folder.
                    var directoryPath = item.Properties.Item("FullPath").Value.ToString();
                    return (false, directoryPath, language, selectedItem);
                }
                else if (item.Kind.Equals(Constants.vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a file; add the .editorconfig file to the same folder.
                    var directoryPath = Path.GetDirectoryName(item.Properties.Item("FullPath").Value.ToString());
                    return (false, directoryPath, language, selectedItem);
                }
            }
            else if (selItem.Object is Project proj && proj.Kind != SolutionFolder) // solution folder
            {
                // The selected item is a project; add the .editorconfig to the project's root.
                var rootFolder = proj.GetRootFolder();
                return (false, rootFolder, language, selectedItem);
            }
        }

        // Default to solution level
        return (true, Path.GetDirectoryName(DTE.Solution.FullName), HasVisualBasicProjects() ? LanguageNames.VisualBasic : LanguageNames.CSharp, selectedItem);
    }

    public static ProjectItem? TryAddFileToHierarchy(this object item, string fileName)
    {
        if (item is Project proj)
        {
            // Added to project
            return proj.TryAddFileToProject(fileName, "None");
        }
        else if (item is ProjectItem projItem && projItem.ContainingProject != null)
        {
            // Added to folder in project
            return projItem.ContainingProject.TryAddFileToProject(fileName, "None");
        }
        else if (item is Solution2 solution)
        {
            // Added to solution
            return solution.TryAddFileToSolution(fileName);
        }

        return null;
    }

    public static ProjectItem? TryAddFileToProject(this Project project, string file, string? itemType = null)
    {
        if (project.IsKind(ProjectTypes.ASPNET_5, ProjectTypes.SSDT))
        {
            return DTE.Solution.FindProjectItem(file);
        }

        var rootFolder = project.GetRootFolder();
        if (rootFolder is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(rootFolder) || !file.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        ProjectItem item = project.ProjectItems.AddFromFile(file);
        if (itemType is not null)
        {
            item.SetItemType(itemType);
        }
        return item;
    }

    public static ProjectItem? TryAddFileToSolution(this Solution2 solution, string fileName)
    {
        Project? currentProject = null;
        foreach (Project project in solution.Projects)
        {
            if (project.Kind == Constants.vsProjectKindSolutionItems && project.Name == "Solution Items")
            {
                currentProject = project;
                break;
            }
        }

        if (currentProject == null)
        {
            currentProject = solution.AddSolutionFolder("Solution Items");
        }

        return currentProject.TryAddFileToProject(fileName, "None");
    }

    public static void OpenFile(string fileName)
    {
        VsShellUtilities.OpenDocument(ServiceProvider.GlobalProvider, fileName);
        Command command = DTE.Commands.Item("SolutionExplorer.SyncWithActiveDocument");
        if (command.IsAvailable)
        {
            DTE.Commands.Raise(command.Guid, command.ID, null, null);
        }
        DTE.ActiveDocument.Activate();
    }

    public static string? GetProjectLanguageName(this Project project)
    {
        return project?.Kind switch
        {
            PrjKind.prjKindCSharpProject => LanguageNames.CSharp,
            PrjKind.prjKindVBProject => LanguageNames.VisualBasic,
            _ => null,
        };
    }

    public static Project? GetVSProject(object item)
    {
        if (item is ProjectItem projectItem)
        {
            return projectItem.ContainingProject;
        }
        else if (item is Project proj && proj.Kind != SolutionFolder) // solution folder
        {
            return proj;
        }

        return null;
    }

    public static string? GetRootFolder(this Project project)
    {
        if (project == null)
        {
            return null;
        }

        if (project.IsKind(SolutionFolder)) // solution folder
        {
            return Path.GetDirectoryName(DTE.Solution.FullName);
        }

        if (string.IsNullOrEmpty(project.FullName))
        {
            return null;
        }

        string? fullPath;

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
            return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;
        }

        if (Directory.Exists(fullPath))
        {
            return fullPath;
        }

        if (File.Exists(fullPath))
        {
            return Path.GetDirectoryName(fullPath);
        }

        return null;
    }

    public static bool IsKind(this Project project, params string[] kindGuids)
    {
        ThreadHelper.ThrowIfNotOnUIThread();
        foreach (var guid in kindGuids)
        {
            if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static void SetItemType(this ProjectItem item, string itemType)
    {
        try
        {
            if (item == null || item.ContainingProject == null || item.Properties == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(itemType) || item.ContainingProject.IsKind(ProjectTypes.WEBSITE_PROJECT, ProjectTypes.UNIVERSAL_APP))
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
}
