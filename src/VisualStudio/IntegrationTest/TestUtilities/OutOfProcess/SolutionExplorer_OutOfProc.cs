// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public partial class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        public Verifier Verify { get; }

        private readonly SolutionExplorer_InProc _inProc;
        private readonly VisualStudioInstance _instance;

        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _inProc = CreateInProcComponent<SolutionExplorer_InProc>(visualStudioInstance);
            Verify = new Verifier(this);
        }

        public void CloseSolution(bool saveFirst = false)
            => _inProc.CloseSolution(saveFirst);

        /// <summary>
        /// The full file path to the solution file.
        /// </summary>
        public string SolutionFileFullPath => _inProc.SolutionFileFullPath;

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
            => _inProc.CreateSolution(solutionName, saveExistingSolutionIfExists);

        public void CreateSolution(string solutionName, XElement solutionElement)
            => _inProc.CreateSolution(solutionName, solutionElement.ToString());

        public void OpenSolution(string path, bool saveExistingSolutionIfExists = false)
            => _inProc.OpenSolution(path, saveExistingSolutionIfExists);

        public void AddProject(ProjectUtils.Project projectName, string projectTemplate, string languageName)
        {
            _inProc.AddProject(projectName.Name, projectTemplate, languageName);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void AddCustomProject(ProjectUtils.Project projectName, string projectFileExtension, string projectFileContent)
        {
            _inProc.AddCustomProject(projectName.Name, projectFileExtension, projectFileContent);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void AddProjectReference(ProjectUtils.Project fromProjectName, ProjectUtils.ProjectReference toProjectName)
        {
            _inProc.AddProjectReference(fromProjectName.Name, toProjectName.Name);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void RemoveProjectReference(ProjectUtils.Project projectName, ProjectUtils.ProjectReference projectReferenceName)
        {
            _inProc.RemoveProjectReference(projectName.Name, projectReferenceName.Name);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void AddMetadataReference(ProjectUtils.AssemblyReference fullyQualifiedAssemblyName, ProjectUtils.Project projectName)
        {
            _inProc.AddMetadataReference(fullyQualifiedAssemblyName.Name, projectName.Name);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void RemoveMetadataReference(ProjectUtils.AssemblyReference assemblyName, ProjectUtils.Project projectName)
        {
            _inProc.RemoveMetadataReference(assemblyName.Name, projectName.Name);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        /// <summary>
        /// Add a PackageReference to the specified project. Generally this should be followed up by
        /// a call to <see cref="RestoreNuGetPackages"/>.
        /// </summary>
        public void AddPackageReference(ProjectUtils.Project project, ProjectUtils.PackageReference package)
            => _inProc.AddPackageReference(project.Name, package.Name, package.Version);

        /// <summary>
        /// Remove a PackageReference from the specified project. Generally this should be followed up by
        /// a call to <see cref="RestoreNuGetPackages"/>.
        /// </summary>
        public void RemovePackageReference(ProjectUtils.Project project, ProjectUtils.PackageReference package)
            => _inProc.RemovePackageReference(project.Name, package.Name);

        public void CleanUpOpenSolution()
            => _inProc.CleanUpOpenSolution();

        public void AddFile(ProjectUtils.Project project, string fileName, string contents = null, bool open = false)
            => _inProc.AddFile(project.Name, fileName, contents, open);

        public void SetFileContents(ProjectUtils.Project project, string fileName, string contents)
            => _inProc.SetFileContents(project.Name, fileName, contents);

        public string GetFileContents(ProjectUtils.Project project, string fileName)
            => _inProc.GetFileContents(project.Name, fileName);

        public void BuildSolution()
            => _inProc.BuildSolution();

        public void OpenFileWithDesigner(ProjectUtils.Project project, string fileName)
            => _inProc.OpenFileWithDesigner(project.Name, fileName);

        public void OpenFile(ProjectUtils.Project project, string fileName)
        {
            // Wireup to open files can happen asynchronously in the case we're being notified of changes on background threads.
            _inProc.OpenFile(project.Name, fileName);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void UpdateFile(string projectName, string fileName, string contents, bool open = false)
            => _inProc.UpdateFile(projectName, fileName, contents, open);

        public void RenameFile(ProjectUtils.Project project, string oldFileName, string newFileName)
        {
            // Wireup to open files can happen asynchronously in the case we're being notified of changes on background threads.
            _inProc.RenameFile(project.Name, oldFileName, newFileName);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void RenameFileViaDTE(ProjectUtils.Project project, string oldFileName, string newFileName)
        {
            // Wireup to open files can happen asynchronously in the case we're being notified of changes on background threads.
            _inProc.RenameFileViaDTE(project.Name, oldFileName, newFileName);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void CloseDesignerFile(ProjectUtils.Project project, string fileName, bool saveFile)
            => _inProc.CloseDesignerFile(project.Name, fileName, saveFile);

        public void CloseCodeFile(ProjectUtils.Project project, string fileName, bool saveFile)
            => _inProc.CloseCodeFile(project.Name, fileName, saveFile);

        public void SaveFile(ProjectUtils.Project project, string fileName)
            => _inProc.SaveFile(project.Name, fileName);

        public void ReloadProject(ProjectUtils.Project project)
            => _inProc.ReloadProject(project.RelativePath);

        public void RestoreNuGetPackages(ProjectUtils.Project project)
            => _inProc.RestoreNuGetPackages(project.Name);

        public void SaveAll()
            => _inProc.SaveAll();

        public void ShowOutputWindow()
            => _inProc.ShowOutputWindow();

        public void UnloadProject(ProjectUtils.Project project)
            => _inProc.UnloadProject(project.Name);

        public string[] GetProjectReferences(ProjectUtils.Project project)
            => _inProc.GetProjectReferences(project.Name);

        public string[] GetAssemblyReferences(ProjectUtils.Project project)
            => _inProc.GetAssemblyReferences(project.Name);

        /// <summary>
        /// Selects an item named by the <paramref name="itemName"/> parameter.
        /// Note that this selects the first item of the given name found. In situations where
        /// there may be more than one item of a given name, use <see cref="SelectItemAtPath(string[])"/>
        /// instead.
        /// </summary>
        public void SelectItem(string itemName)
            => _inProc.SelectItem(itemName);

        /// <summary>
        /// Selects the specific item at the given "path".
        /// </summary>
        public void SelectItemAtPath(params string[] path)
            => _inProc.SelectItemAtPath(path);

        /// <summary>
        /// Returns the names of the immediate children of the given item.
        /// Note that this uses the first item of the given name found. In situations where there
        /// may be more than one item of a given name, use <see cref="GetChildrenOfItemAtPath(string[])"/>
        /// instead.
        /// </summary>
        public string[] GetChildrenOfItem(string itemName)
            => _inProc.GetChildrenOfItem(itemName);

        /// <summary>
        /// Returns the names of the immediate children of the item at the given "path".
        /// </summary>
        public string[] GetChildrenOfItemAtPath(params string[] path)
            => _inProc.GetChildrenOfItemAtPath(path);

        public void ClearBuildOutputWindowPane()
            => _inProc.ClearBuildOutputWindowPane();

        public void WaitForBuildToFinish()
            => _inProc.WaitForBuildToFinish();

        public void EditProjectFile(ProjectUtils.Project project)
            => _inProc.EditProjectFile(project.Name);

        public void AddStandaloneFile(string fileName)
            => _inProc.AddStandaloneFile(fileName);

        public void BeginWatchForCodingConventionsChange(ProjectUtils.Project project, string fileName)
            => _inProc.BeginWatchForCodingConventionsChange(project.Name, fileName);

        public void EndWaitForCodingConventionsChange(TimeSpan timeout)
            => _inProc.EndWaitForCodingConventionsChange(timeout);
    }
}
