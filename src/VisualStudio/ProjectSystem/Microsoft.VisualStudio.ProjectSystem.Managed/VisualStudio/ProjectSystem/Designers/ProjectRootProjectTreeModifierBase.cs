// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.Imaging.Interop;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides the base class for tree modifiers that handle the project root.
    /// </summary>
    internal abstract class ProjectRootProjectTreeModifierBase : ProjectTreeModifierBase
    {
        protected ProjectRootProjectTreeModifierBase()
        {
        }

        public abstract ImageMoniker ProjectRootIcon
        {
            get;
        }

        public override IProjectTree ApplyModifications(IProjectTree tree, IProjectTree previousTree, IProjectTreeProvider projectTreeProvider)
        {
            // Are we initializing the project root?
            if (previousTree == null && tree.IsProjectRoot())
            {
                ProjectImageMoniker icon = ProjectRootIcon.ToProjectSystemType();

                if (tree.Icon != icon)
                {
                    tree = tree.SetIcon(icon);
                }
            }

            return tree;
        }
    }
}

