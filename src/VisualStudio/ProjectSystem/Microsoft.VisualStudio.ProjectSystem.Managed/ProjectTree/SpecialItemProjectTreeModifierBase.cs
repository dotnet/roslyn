// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.ProjectTree
{
    /// <summary>
    ///     Provides the base class for <see cref="IProjectTreeModifier"/> objects that handle special folders, such as the Properties "AppDesigner" folder.
    /// </summary>
    internal abstract class SpecialItemProjectTreeModifierBase : ProjectTreeModifierBase
    {
        protected SpecialItemProjectTreeModifierBase()
        {
        }

        public abstract ImageMoniker Icon
        {
            get;
        }

        public abstract ImmutableHashSet<string> DefaultCapabilities
        {
            get;
        }

        public abstract bool HideChildren
        {
            get;
        }

        public override sealed IProjectTree ApplyModifications(IProjectTree tree, IProjectTree previousTree, IProjectTreeProvider projectTreeProvider)
        {
            if (!tree.IsProjectRoot())
                return tree;

            IProjectTree item = FindCandidateSpecialItem(tree);
            if (item == null)
                return tree;

            Assumes.True(item.Parent.IsProjectRoot(), "Expected returned item to be rooted by Project");

            ProjectImageMoniker icon = Icon.ToProjectSystemType();

            item = item.SetProperties(
                        icon: icon,
                        resetIcon: icon == null,
                        expandedIcon: icon,
                        resetExpandedIcon: icon == null,
                        capabilities: DefaultCapabilities.Union(item.Capabilities));

            if (HideChildren)
            {
                item = HideAllChildren(item);
            }

            return item.Parent;
        }

        protected abstract IProjectTree FindCandidateSpecialItem(IProjectTree projectRoot);

        private IProjectTree HideAllChildren(IProjectTree tree)
        {
            for (int i = 0; i < tree.Children.Count; i++)
            {
                var child = tree.Children[i].AddCapability(ProjectTreeCapabilities.VisibleOnlyInShowAllFiles);
                child = this.HideAllChildren(child);
                tree = child.Parent;
            }

            return tree;
        }
    }
}
