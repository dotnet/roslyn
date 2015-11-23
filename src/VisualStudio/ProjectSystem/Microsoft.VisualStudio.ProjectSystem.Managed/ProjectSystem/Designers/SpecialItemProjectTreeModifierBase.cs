// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;
using Microsoft.VisualStudio.ProjectSystem.Utilities.Designers;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides the base class for <see cref="IProjectTreeModifier"/> objects that handle special items, such as the AppDesigner folder.
    /// </summary>
    internal abstract class SpecialItemProjectTreeModifierBase : ProjectTreeModifierBase
    {
        private readonly IProjectImageProvider _imageProvider;

        protected SpecialItemProjectTreeModifierBase(IProjectImageProvider imageProvider)
        {
            Requires.NotNull(imageProvider, nameof(imageProvider));

            _imageProvider = imageProvider;
        }

        /// <summary>
        ///     Gets the image key that represents the image that will be applied to the candidate special item.
        /// </summary>
        public abstract string ImageKey
        {
            get;
        }

        /// <summary>
        ///     Gets the default capabilities that will be applied to the candidate special item.
        /// </summary>
        public abstract ImmutableHashSet<string> DefaultCapabilities
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating whether the special item is supported in this project.
        /// </summary>
        public abstract bool IsSupported
        {
            get;
        }

        /// <summary>
        ///     Gets a value indicating whether the special item is expandable by default.
        /// </summary>
        public abstract bool IsExpandableByDefault
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

            if (!IsExpandableByDefault)
            {
                item = HideAllChildren(item);
            }

            return item.Root;
        }

        /// <summary>
        ///     Returns a candidate of the special item, or <see langword="null"/> if not found.
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
            return _imageProvider.GetProjectImage(ImageKey);
        }
    }
}
