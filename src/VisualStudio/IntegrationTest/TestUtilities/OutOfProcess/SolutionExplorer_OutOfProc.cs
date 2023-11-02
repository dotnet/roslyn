// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.IntegrationTest.Utilities.InProcess;
using ProjectUtils = Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.OutOfProcess
{
    public class SolutionExplorer_OutOfProc : OutOfProcComponent
    {
        private readonly SolutionExplorer_InProc _inProc;
        private readonly VisualStudioInstance _instance;

        public SolutionExplorer_OutOfProc(VisualStudioInstance visualStudioInstance)
            : base(visualStudioInstance)
        {
            _instance = visualStudioInstance;
            _inProc = CreateInProcComponent<SolutionExplorer_InProc>(visualStudioInstance);
        }

        /// <summary>
        /// Creates and loads a new solution in the host process, optionally saving the existing solution if one exists.
        /// </summary>
        public void CreateSolution(string solutionName, bool saveExistingSolutionIfExists = false)
            => _inProc.CreateSolution(solutionName, saveExistingSolutionIfExists);

        public void AddProject(ProjectUtils.Project projectName, string projectTemplate, string languageName)
        {
            _inProc.AddProject(projectName.Name, projectTemplate, languageName);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void AddMetadataReference(ProjectUtils.AssemblyReference fullyQualifiedAssemblyName, ProjectUtils.Project projectName)
        {
            _inProc.AddMetadataReference(fullyQualifiedAssemblyName.Name, projectName.Name);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void CleanUpOpenSolution()
            => _inProc.CleanUpOpenSolution();

        public void AddFile(ProjectUtils.Project project, string fileName, string? contents = null, bool open = false)
            => _inProc.AddFile(project.Name, fileName, contents, open);

        public string GetFileContents(ProjectUtils.Project project, string fileName)
            => _inProc.GetFileContents(project.Name, fileName);

        public void OpenFile(ProjectUtils.Project project, string fileName)
        {
            // Wireup to open files can happen asynchronously in the case we're being notified of changes on background threads.
            _inProc.OpenFile(project.Name, fileName);
            _instance.Workspace.WaitForAsyncOperations(Helper.HangMitigatingTimeout, FeatureAttribute.Workspace);
        }

        public void RestoreNuGetPackages(ProjectUtils.Project project)
            => _inProc.RestoreNuGetPackages(project.Name);

        /// <summary>
        /// Selects an item named by the <paramref name="itemName"/> parameter.
        /// Note that this selects the first item of the given name found.
        /// </summary>
        public void SelectItem(string itemName)
            => _inProc.SelectItem(itemName);
    }
}
