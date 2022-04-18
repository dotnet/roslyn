// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Templates.Editorconfig.Wizard;
using Constants = EnvDTE.Constants;
using Project = EnvDTE.Project;

namespace Templates.EditorConfig.FileGenerator;

internal class EditorConfigFileGenerator
{
    private readonly DTE2 _dte;

    public EditorConfigFileGenerator(DTE2 dte)
    {
        this._dte = dte;
    }

    internal (bool success, string fileName) TryGenerateFile(bool isDotnet)
    {
        var (success, path, isAtSolutionLevel, selectedItem) = TryGetSelectedItemAndPath();
        if (!success)
        {
            return (false, null);
        }

        var language = GetContainingProjectLanguage(selectedItem);

        var createFileResult = TryCreateFile(path, isDotnet, isAtSolutionLevel, language);
        if (!createFileResult.success)
        {
            return (false, null);
        }

        var fileHierarchyResult = TryAddFileToHierarchy(selectedItem, createFileResult.fileName);
        if (fileHierarchyResult.success)
        {
            return (fileHierarchyResult.projectItem != null, createFileResult.fileName);
        }

        return (false, null);
    }

    private (bool success, string path, bool isAtSolutionLevel, object selectedItem) TryGetSelectedItemAndPath()
    {
        var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;
        object selectedItem = null;

        foreach (UIHierarchyItem selItem in items)
        {
            selectedItem = selItem.Object;
            if (selItem.Object is ProjectItem item && item.Properties != null)
            {
                if (item.Kind.Equals(Constants.vsProjectItemKindPhysicalFolder, StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a folder; add the .editorconfig file to the folder.
                    var directoryPath = item.Properties.Item("FullPath").Value.ToString();
                    return (selectedItem != null, directoryPath, false, selectedItem);
                }
                else if (item.Kind.Equals(Constants.vsProjectItemKindPhysicalFile, StringComparison.OrdinalIgnoreCase))
                {
                    // The selected item is a file; add the .editorconfig file to the same folder.
                    var directoryPath = Path.GetDirectoryName(item.Properties.Item("FullPath").Value.ToString());
                    return (selectedItem != null, directoryPath, false, selectedItem);
                }
            }
            else if (selItem.Object is Project proj && proj.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // solution folder
            {
                // The selected item is a project; add the .editorconfig to the project's root.
                (bool success, string rootFolder) = proj.TryGetRootFolder(_dte.Solution.FullName);
                return (success && selectedItem != null, rootFolder, true, selectedItem);
            }
        }

        return (selectedItem != null, Path.GetDirectoryName(_dte.Solution.FullName), false, selectedItem);
    }

    string GetContainingProjectLanguage(object item)
    {
        var projectItem = GetVSProject(item);
        if (projectItem is null)
        {
            return null;
        }

        var serviceProvider = new ServiceProvider(_dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
        var solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
        if (solution is null)
        {
            return null;
        }

        if(solution.GetProjectOfUniqueName(projectItem.UniqueName, out var hierarchy) != 0)
        {
            return null;
        }

        var componentModel = serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
        var workspace = componentModel?.GetService<VisualStudioWorkspace>();
        var project = workspace?.CurrentSolution.Projects.FirstOrDefault(proj => workspace.GetHierarchy(proj.Id) == hierarchy);
        return project?.Language;
        
        static Project GetVSProject(object item)
        {
            if (item is ProjectItem projectItem)
            {
                return projectItem.ContainingProject;
            }
            else if (item is Project proj && proj.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // solution folder
            {
                return proj;
            }

            return null;
        }
    }

    private (bool success, string fileName) TryCreateFile(string projectPath, bool isDotnet, bool isAtSolutionLevel, string language)
    {
        string fileName = Path.Combine(projectPath, TemplateConstants.FileName);
        if (File.Exists(fileName))
        {

            MessageBox.Show(WizardResource.AlreadyExists, ".editorconfig item template", MessageBoxButton.OK, MessageBoxImage.Information);
            return (false, null);
        }
        else
        {
            WriteFile(fileName, isDotnet, isAtSolutionLevel, language);
            return (true, fileName);
        }
    }

    private void WriteFile(string fileName, bool isDotnet, bool isAtSolutionLevel, string language)
    {
        string editorconfigFileContents = language switch
        {
            LanguageNames.CSharp => GenerateCSharpFromVsSetting(),
            LanguageNames.VisualBasic => GenerateVisualBasicFromVsSetting(),
            _ => null
        };

        if (editorconfigFileContents is null)
        {
            // fall back to static content if we cannot dyamically generate it
            editorconfigFileContents = (isDotnet, isAtSolutionLevel) switch
            {
                (true, true) => TemplateConstants.DotNetFileContentIsRoot,
                (true, false) => TemplateConstants.DefaultFileContentIsRoot,
                (false, true) => TemplateConstants.DotNetFileContent,
                (false, false) => TemplateConstants.DefaultFileContent,
            };
        }

        File.WriteAllText(fileName, editorconfigFileContents);

        string GenerateCSharpFromVsSetting()
        {
            var optionSet = EditorConfigFileGeneratorShim.GetEditorConfigOptionSet(_dte);
            if (optionSet is null)
            {
                return null;
            }

            var  (success, options) = EditorConfigFileGeneratorShim.GetEditorConfigOptionsCSharp(_dte);
            if (!success)
            {
                return null;
            }

            return EditorConfigFileGeneratorShim.Generate(options, optionSet, LanguageNames.CSharp);
        }

        string GenerateVisualBasicFromVsSetting()
        {
            var optionSet = EditorConfigFileGeneratorShim.GetEditorConfigOptionSet(_dte);
            if (optionSet is null)
            {
                return null;
            }

            var (success, options) = EditorConfigFileGeneratorShim.GetEditorConfigOptionsVisualBasic(_dte);
            if (!success)
            {
                return null;
            }
            
            return EditorConfigFileGeneratorShim.Generate(options, optionSet, LanguageNames.VisualBasic);
        }
    }

    private (bool success, ProjectItem projectItem) TryAddFileToHierarchy(object item, string fileName)
    {
        if (item is Project proj)
        {
            // Added to project
            return proj.TryAddFileToProject(_dte, fileName, "None");
        }
        else if (item is ProjectItem projItem && projItem.ContainingProject != null)
        {
            // Added to folder in project
            return projItem.ContainingProject.TryAddFileToProject(_dte, fileName, "None");
        }
        else if (item is Solution2 solution)
        {
            // Added to solution
            return solution.TryAddFileToSolution(_dte, fileName);
        }

        return (false, null);
    }

    internal void OpenFile(string fileName)
    {
        var serviceProvider = new ServiceProvider(_dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
        VsShellUtilities.OpenDocument(serviceProvider, fileName);
        Command command = _dte.Commands.Item("SolutionExplorer.SyncWithActiveDocument");
        if (command.IsAvailable)
        {
            _dte.Commands.Raise(command.Guid, command.ID, null, null);
        }
        _dte.ActiveDocument.Activate();
    }
}
