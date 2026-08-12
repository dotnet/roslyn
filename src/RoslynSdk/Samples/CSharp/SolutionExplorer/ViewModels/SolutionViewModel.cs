using System.IO;
using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class SolutionViewModel : HierarchyItemViewModel
    {
        public SolutionViewModel(Workspace workspace)
            : base(workspace)
        {
            Solution solution = workspace.CurrentSolution;
            foreach (ProjectId projectId in solution.ProjectIds)
            {
                AddChild(new ProjectViewModel(workspace, projectId));
            }
        }

        protected override string GetDisplayName()
            => Path.GetFileNameWithoutExtension(Workspace.CurrentSolution.FilePath);
    }
}
