// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.ProjectSystem.Utilities;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ProjectSystem.Designers.Imaging;

namespace Microsoft.VisualStudio.ProjectSystem.Designers
{
    /// <summary>
    ///     Provides the base class for tree modifiers that handle the project root.
    /// </summary>
    [Export(typeof(IProjectTreeModifier))]
    [AppliesTo(ProjectCapability.CSharpOrVisualBasic)]
    internal class ProjectRootImageProjectTreeModifier : ProjectTreeModifierBase
    {
        private readonly IProjectImageProvider _provider;

        [ImportingConstructor]
        public ProjectRootImageProjectTreeModifier([Import(typeof(ProjectImageProviderAggregator))]IProjectImageProvider provider)
        {
            Requires.NotNull(provider, nameof(provider));

            _provider = provider;
        }

        public override IProjectTree ApplyModifications(IProjectTree tree, IProjectTree previousTree, IProjectTreeProvider projectTreeProvider)
        {
            // We're not initializing, don't update the icon
            if (previousTree != null)
                return tree;

            if (!tree.IsProjectRoot())
                return tree;

            ProjectImageMoniker icon;
            if (!_provider.TryGetProjectImage(ProjectImageKey.ProjectRoot, out icon) || tree.Icon == icon)
                return tree;

            return tree.SetIcon(icon);
        }
    }
}

