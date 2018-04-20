// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Templates.Editorconfig.Wizard;
using System.Windows;

namespace Templates.EditorConfig.FileGenerator
{
    internal class EditorConfigFileGenerator
    {
        private DTE2 _dte;

        public EditorConfigFileGenerator(DTE2 dte)
        {
            this._dte = dte;
        }

        internal (bool success, string fileName) TryGenerateFile(bool isDotnet)
        {
            var (success, path, selectedItem) = TryGetSelectedItemAndPath();
            if (!success)
            {
                return (false, null);
            }

            var createFileResult = TryCreateFile(path, isDotnet);
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

        private (bool success, string path, object selectedItem) TryGetSelectedItemAndPath()
        {
            var items = (Array)_dte.ToolWindows.SolutionExplorer.SelectedItems;
            object selectedItem = null;

            foreach (UIHierarchyItem selItem in items)
            {
                selectedItem = selItem.Object;
                if (selItem.Object is ProjectItem item && item.Properties != null)
                {
                    return (selectedItem != null, item.Properties.Item("FullPath").Value.ToString(), selectedItem);
                }
                else if (selItem.Object is Project proj && proj.Kind != "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}") // solution folder
                {
                    (bool success, string rootFolder) = proj.TryGetRootFolder(_dte.Solution.FullName);
                    return (success && selectedItem != null, rootFolder, selectedItem);
                }
            }

            return (selectedItem != null, Path.GetDirectoryName(_dte.Solution.FullName), selectedItem);
        }

        private (bool success, string fileName) TryCreateFile(string projectPath, bool isDotnet)
        {
            string fileName = Path.Combine(projectPath, TemplateConstants.FileName);
            if (File.Exists(fileName))
            {
                MessageBox.Show("An .editorconfig file already exist in this location", ".editorconfig item template", MessageBoxButton.OK, MessageBoxImage.Information);
                return (false, null);
            }
            else
            {
                if (isDotnet)
                {
                    File.WriteAllText(fileName, TemplateConstants.DotNetFileContent);
                    return (true, fileName);
                }
                else
                {
                    File.WriteAllText(fileName, TemplateConstants.DefaultFileContent);
                    return (true, fileName);
                }
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
}
