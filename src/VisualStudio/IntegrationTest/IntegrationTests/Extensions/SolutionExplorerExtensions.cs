// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Roslyn.VisualStudio.IntegrationTests.Extensions.SolutionExplorer
{
    public static partial class SolutionExplorerExtensions
    {
        public static void CreateSolution(this AbstractIntegrationTest test, string solutionName, bool saveExistingSolutionIfExists = false)
            => test.VisualStudio.Instance.SolutionExplorer.CreateSolution(solutionName, saveExistingSolutionIfExists);

        public static void CloseSolution(this AbstractIntegrationTest test, bool saveFirst = false)
            => test.VisualStudio.Instance.SolutionExplorer.CloseSolution(saveFirst);

        public static void BuildSolution(this AbstractIntegrationTest test, bool waitForBuildToFinish)
            => test.VisualStudio.Instance.SolutionExplorer.BuildSolution(waitForBuildToFinish);

        public static void AddProject(this AbstractIntegrationTest test, string projectTemplate, ProjectUtils.Project project, string languageName)
            => test.VisualStudio.Instance.SolutionExplorer.AddProject(project.Name, projectTemplate, languageName);

        public static void AddFile(this AbstractIntegrationTest test, string fileName, ProjectUtils.Project project, string contents = null, bool open = false)
            => test.VisualStudio.Instance.SolutionExplorer.AddFile(project.Name, fileName, contents, open);

        public static void AddReference(this AbstractIntegrationTest test, string projectName, string fullyQualifiedAssemblyName)
         => test.VisualStudio.Instance.SolutionExplorer.AddReference(projectName, fullyQualifiedAssemblyName);

        public static void AddMetadataReference(this AbstractIntegrationTest test, ProjectUtils.AssemblyReference referenceName, ProjectUtils.Project projectName)
        {
            test.VisualStudio.Instance.SolutionExplorer.AddMetadataReference(referenceName.Name, projectName.Name);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void RemoveMetadataReference(this AbstractIntegrationTest test, ProjectUtils.AssemblyReference referenceName, ProjectUtils.Project projectName)
        {
            test.VisualStudio.Instance.SolutionExplorer.RemoveMetadataReference(referenceName.Name, projectName.Name);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void AddProjectReference(this AbstractIntegrationTest test, ProjectUtils.Project fromProjectName, ProjectUtils.ProjectReference toProjectName)
        {
            test.VisualStudio.Instance.SolutionExplorer.AddProjectReference(fromProjectName.Name, toProjectName.Name);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void RemoveProjectReference(this AbstractIntegrationTest test, ProjectUtils.ProjectReference projectReferenceName, ProjectUtils.Project projectName)
        {
            test.VisualStudio.Instance.SolutionExplorer.RemoveProjectReference(projectName.Name, projectReferenceName.Name);
            test.WaitForAsyncOperations(FeatureAttribute.Workspace);
        }

        public static void OpenFile(this AbstractIntegrationTest test, string fileName, ProjectUtils.Project project)
            => test.VisualStudio.Instance.SolutionExplorer.OpenFile(project.Name, fileName);

        public static void OpenFileWithDesigner(this AbstractIntegrationTest test, string fileName, ProjectUtils.Project project)
            => test.VisualStudio.Instance.SolutionExplorer.OpenFileWithDesigner(project.Name, fileName);

        public static void CloseFile(this AbstractIntegrationTest test, string fileName, ProjectUtils.Project project, bool saveFile = true)
            => test.VisualStudio.Instance.SolutionExplorer.CloseFile(project.Name, fileName, saveFile);

        public static void SaveFile(this AbstractIntegrationTest test, string fileName, ProjectUtils.Project project)
            => test.VisualStudio.Instance.SolutionExplorer.SaveFile(project.Name, fileName);

        public static void SaveAll(this AbstractIntegrationTest test)
            => test.VisualStudio.Instance.SolutionExplorer.SaveAll();

        public static void EditProjectFile(this AbstractIntegrationTest test, ProjectUtils.Project project)
            => test.VisualStudio.Instance.SolutionExplorer.EditProjectFile(project.Name);

        public static void CloseFile(this AbstractIntegrationTest test, string projectName, string fileName, bool saveFile = true)
            => test.VisualStudio.Instance.SolutionExplorer.CloseFile(projectName, fileName, saveFile);

        public static void SaveFile(this AbstractIntegrationTest test, string projectName, string fileName)
            => test.VisualStudio.Instance.SolutionExplorer.SaveFile(projectName, fileName);
    }
}