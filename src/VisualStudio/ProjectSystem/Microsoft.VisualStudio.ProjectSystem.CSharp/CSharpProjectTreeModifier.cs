// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Designers;
using Microsoft.VisualStudio.ProjectSystem.Utilities;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.CSharp
{
    /// <summary>
    /// Applies C#-specific project item icons.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapabilities.CSharp)]
    internal class CSharpProjectTreeModifier : IProjectTreeModifier, IProjectTreeModifier2
    {
        /// <summary>
        /// The recognized name for the Properties folder.
        /// </summary>
        private const string PropertiesFolderName = "Properties";

        /// <summary>
        /// A common set of project tree capabilities with the case-insensitive comparer.
        /// </summary>
        private static readonly ImmutableHashSet<string> DefaultAppDesignerFolderCapabilities =
            ProjectTreeCapabilities.EmptyCapabilities.Add(ProjectTreeCapabilities.AppDesignerFolder).Add(ProjectTreeCapabilities.BubbleUp);

        /// <summary>
        /// Get the unconfigured project
        /// </summary>
        [Import]
        private UnconfiguredProject UnconfiguredProject { get; set; }

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
                // NOTE: shared project sets its own project icon, so we don't want to fight with it and change between two icons all the time.
                if (tree.Icon != KnownMonikers.CSProjectNode.ToProjectSystemType() &&
                    previousTree == null &&
                    !this.UnconfiguredProject.IsProjectCapabilityPresent(ProjectCapabilities.SharedAssetsProject))
                {
                    tree = tree.SetIcon(KnownMonikers.CSProjectNode.ToProjectSystemType());
                }

                IProjectTree propertiesFolder = tree.Children.FirstOrDefault(n => string.Equals(n.Caption, PropertiesFolderName, StringComparison.OrdinalIgnoreCase) && n.IsFolder);
                if (propertiesFolder != null && !propertiesFolder.Capabilities.Contains(ProjectTreeCapabilities.AppDesignerFolder))
                {
                    var icon = KnownMonikers.Property.ToProjectSystemType();
                    var expandedIcon = KnownMonikers.FolderOpened.ToProjectSystemType();

                    var newPropertiesFolder = propertiesFolder
                        .SetProperties(
                            icon: icon,
                            resetIcon: icon == null,
                            expandedIcon: expandedIcon,
                            resetExpandedIcon: expandedIcon == null,
                            capabilities: DefaultAppDesignerFolderCapabilities.Union(propertiesFolder.Capabilities));
                    tree = newPropertiesFolder.Root;
                }
            }

            return tree;
        }
    }
}
