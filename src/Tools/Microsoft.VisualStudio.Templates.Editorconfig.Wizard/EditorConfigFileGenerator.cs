// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard;
using System.IO;
using System.Linq;
using System.Windows;

namespace Templates.EditorConfig.FileGenerator;

public static class EditorConfigFileGenerator
{
    public static (bool success, string fileName) TryAddFileToSolution(bool? isDotnet = null)
    {
        bool hasDotNetProjects = isDotnet ?? VSHelpers.IsDotnet();
        var (isAtSolutionLevel, path, language, selectedItem) = VSHelpers.TryGetSelectedItemLanguageAndPath();
        if (path is null || selectedItem is null)
        {
            return (false, null);
        }

        var (success1, fileName) = TryCreateFile(path, hasDotNetProjects, isAtSolutionLevel, language);
        if (!success1)
        {
            return (false, null);
        }

        var projectItem = selectedItem.TryAddFileToHierarchy(fileName);
        if (projectItem is null)
        {
            return (false, fileName);
        }

        return (false, null);
    }

    public static (bool success, string fileName) TryAddFileToFolder(string directory)
    {
        bool isDotnet = VSHelpers.IsDotnet(directory);
        var language = VSHelpers.GetLanguageFromDirectory(directory);

        var (success, fileName) = TryCreateFile(directory, isDotnet, true, language);
        if (!success)
        {
            return (true, fileName);
        }

        return (false, null);
    }

    private static (bool success, string fileName) TryCreateFile(string projectPath, bool isDotnet, bool isAtSolutionLevel, string language)
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

    private static void WriteFile(string fileName, bool isDotnet, bool isAtSolutionLevel, string language)
    {
        string editorconfigFileContents = GetEditorconfigFileContents(isDotnet, isAtSolutionLevel, language);
        File.WriteAllText(fileName, editorconfigFileContents);

        static string GetEditorconfigFileContents(bool isDotnet, bool isAtSolutionLevel, string language)
        {
            if (!isDotnet)
            {
                return isAtSolutionLevel switch
                {
                    true => TemplateConstants.DefaultFileContentIsRoot,
                    false => TemplateConstants.DefaultFileContent,
                };
            }

            if (language is null)
            {
                return isAtSolutionLevel switch
                {
                    true => TemplateConstants.DotNetFileContentIsRoot,
                    false => TemplateConstants.DotNetFileContent,
                };
            }

            var generator = new RoslynEditorConfigFileGenerator(VSHelpers.DTE);
            return language switch
            {
                LanguageNames.CSharp => generator.Generate(LanguageNames.CSharp),
                LanguageNames.VisualBasic => generator.Generate(LanguageNames.VisualBasic),
                _ => null
            };
        }
    }
}
