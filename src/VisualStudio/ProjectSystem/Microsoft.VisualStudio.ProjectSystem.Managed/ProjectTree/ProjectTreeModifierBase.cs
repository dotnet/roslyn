// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectTree
{
    /// <summary>
    ///     Provides a base class for all project tree modifiers.
    /// </summary>
    internal abstract class ProjectTreeModifierBase : IProjectTreeModifier, IProjectTreeModifier2
    {
        protected ProjectTreeModifierBase()
        {
        }

        public IProjectTree ApplyModifications(IProjectTree tree, IProjectTreeProvider projectTreeProvider)
        {
            return ApplyModifications(tree, (IProjectTree)null, projectTreeProvider);
        }

        public abstract IProjectTree ApplyModifications(IProjectTree tree, IProjectTree previousTree, IProjectTreeProvider projectTreeProvider);
    }
}
