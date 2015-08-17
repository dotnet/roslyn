// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.VisualBasic
{
    /// <summary>
    /// Applies VB-specific project item icons.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.VB)]
    internal class VisualBasicProjectTreeModifier : IProjectTreeModifier, IProjectTreeModifier2
    {
        /// <summary>
        /// The recognized name for the "My Project" folder.
        /// </summary>
        private const string MyProjectFolderName = "My Project";

        /// <summary>
        /// A common set of project tree capabilities with the case-insensitive comparer.
        /// </summary>
        private static readonly ImmutableHashSet<string> DefaultAppDesignerFolderCapabilities =
            ProjectTreeCapabilities.EmptyCapabilities.Add(ProjectTreeCapabilities.AppDesignerFolder).Add(ProjectTreeCapabilities.BubbleUp);

        /// <summary>
        /// Transforms a tree in some way (perhaps customizing icons or menus).
        /// </summary>
        /// <param name="tree">The tree or node as it comes from a tree provider or another modifier.</param>
        /// <param name="projectTreeProvider">The project tree provider that created the initial tree node.</param>
        /// <returns>The modified tree, or the original tree if no changes were appropriate.  It node's identity must match the one passed in.</returns>
        public IProjectTree ApplyModifications(IProjectTree tree, IProjectTreeProvider projectTreeProvider)
        {
            return this.ApplyModifications(tree, null, projectTreeProvider);
        }

        /// <inheritdoc/>
        /// <see cref="IProjectTreeModifier2.ApplyModifications(IProjectTree, IProjectTree, IProjectTreeProvider)"/>
        public IProjectTree ApplyModifications(IProjectTree tree, IProjectTree previousTree, IProjectTreeProvider projectTreeProvider)
        {
            if (tree.Capabilities.Contains(ProjectTreeCapabilities.ProjectRoot))
            {
                if (tree.Icon != KnownMonikers.VBProjectNode.ToProjectSystemType() && previousTree == null)
                {
                    tree = tree.SetIcon(KnownMonikers.VBProjectNode.ToProjectSystemType());
                }

                IProjectTree myProjectFolder = tree.Children.FirstOrDefault(n => string.Equals(n.Caption, MyProjectFolderName, StringComparison.OrdinalIgnoreCase) && n.IsFolder);
                if (myProjectFolder != null && !myProjectFolder.Capabilities.Contains(ProjectTreeCapabilities.AppDesignerFolder))
                {
                    var icon = KnownMonikers.Property.ToProjectSystemType();
                    var expandedIcon = KnownMonikers.FolderOpened.ToProjectSystemType();

                    var newMyProjectFolder = myProjectFolder
                        .SetProperties(
                            icon: icon,
                            resetIcon: icon == null,
                            expandedIcon: expandedIcon,
                            resetExpandedIcon: expandedIcon == null,
                            capabilities: DefaultAppDesignerFolderCapabilities);

                    newMyProjectFolder = this.HideAllChildren(newMyProjectFolder);

                    tree = myProjectFolder.Replace(newMyProjectFolder).Root;
                }
            }

            return tree;
        }

        /// <summary>
        /// Hides any visible children for a given tree node.
        /// </summary>
        /// <param name="tree">The tree whose child nodes whould be hidden.</param>
        /// <returns>The modified tree.</returns>
        private IProjectTree HideAllChildren(IProjectTree tree)
        {
            for (int i = 0; i < tree.Children.Count; i++)
            {
                var child = tree.Children[i].SetVisible(false);
                child = this.HideAllChildren(child);
                tree = child.Parent;
            }

            return tree;
        }
    }
}
