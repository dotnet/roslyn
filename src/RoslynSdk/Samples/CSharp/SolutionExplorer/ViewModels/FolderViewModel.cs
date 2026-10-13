// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
