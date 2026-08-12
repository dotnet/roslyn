using Microsoft.CodeAnalysis;

namespace MSBuildWorkspaceTester.ViewModels
{
    internal class FolderViewModel : HierarchyItemViewModel
    {
        private readonly string _displayName;

        public FolderViewModel(Workspace workspace, string displayName) : base(workspace, isExpanded: false)
        {
            _displayName = displayName;
        }

        protected override string GetDisplayName()
        {
            return _displayName;
        }

        public override int CompareTo(HierarchyItemViewModel other)
        {
            if (other is ReferencesFolderViewModel)
            {
                return 1;
            }

            if (other is DocumentViewModel)
            {
                return -1;
            }

            return base.CompareTo(other);
        }

    }
}
