// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides the base class for <see cref="IProjectTreeModifier"/> objects that handle special folders, such as the Properties "AppDesigner" folder.
    /// </summary>
    internal abstract class SpecialItemProjectTreeModifierBase : ProjectTreeModifierBase
    {
        private readonly IProjectImageProvider _imageProvider;

        protected SpecialItemProjectTreeModifierBase(IProjectImageProvider imageProvider)
        {
            Requires.NotNull(imageProvider, nameof(imageProvider));

            _imageProvider = imageProvider;
        }

        public abstract string ImageKey
        {
            get;
        }

        public abstract ImmutableHashSet<string> DefaultCapabilities
        {
            get;
        }

        public abstract bool IsSupported
        {
            get;
        }

        public abstract bool IsExpandable
        {
            get;
        }

        protected override sealed IProjectTree ApplyModificationsToCompletedTree(IProjectTree projectRoot)
        {
            if (!IsSupported)
                return projectRoot;

            IProjectTree item = FindCandidateSpecialItem(projectRoot);
            if (item == null)
                return projectRoot;

            ProjectImageMoniker icon = GetSpecialItemIcon();

            item = item.SetProperties(
                        icon: icon,
                        resetIcon: icon == null,
                        expandedIcon: icon,
                        resetExpandedIcon: icon == null,
                        capabilities: DefaultCapabilities.Union(item.Capabilities));

            if (!IsExpandable)
            {
                item = HideAllChildren(item);
            }

            return item.Root;
        }

        /// <summary>
        ///     Returns the candidate of the special item, or null if not found.
        /// </summary>
        protected abstract IProjectTree FindCandidateSpecialItem(IProjectTree projectRoot);

        private IProjectTree HideAllChildren(IProjectTree tree)
        {
            for (int i = 0; i < tree.Children.Count; i++)
            {
                var child = tree.Children[i].AddCapability(ProjectTreeCapabilities.VisibleOnlyInShowAllFiles);
                child = HideAllChildren(child);
                tree = child.Parent;
            }

            return tree;
        }

        private ProjectImageMoniker GetSpecialItemIcon()
        {
            ProjectImageMoniker moniker;
            if (_imageProvider.TryGetProjectImage(ImageKey, out moniker))
                return moniker;

            return null;
        }
    }
}
