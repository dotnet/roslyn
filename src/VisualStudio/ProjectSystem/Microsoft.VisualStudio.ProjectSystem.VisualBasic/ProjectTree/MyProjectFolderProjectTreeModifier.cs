// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.ProjectTree;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic.ProjectTree
{
    /// <summary>
    ///     A tree modifier that turns "My Project" folder into a special folder.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class MyProjectFolderProjectTreeModifier : AppDesignerFolderProjectTreeModifierBase
    {
        [ImportingConstructor]
        public MyProjectFolderProjectTreeModifier()
        {
        }

        public override bool HideChildren
        {
            get { return true; }
        }

        protected override string GetAppDesignerFolderName()
        {
            string folderName = base.GetAppDesignerFolderName();
            if (!string.IsNullOrEmpty(folderName))
                return folderName;

            return "My Project";        // Not localized
        }
    }
}
