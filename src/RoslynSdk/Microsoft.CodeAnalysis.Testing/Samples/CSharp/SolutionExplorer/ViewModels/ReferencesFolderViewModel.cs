using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class ReferencesFolderViewModel : HierarchyItemViewModel
    {
        public ReferencesFolderViewModel(Workspace workspace)
            : base(workspace, isExpanded: false)
        {
        }

        protected override string GetDisplayName() => "References";
    }
}
