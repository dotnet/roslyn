// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
