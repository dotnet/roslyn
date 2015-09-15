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

        protected override IProjectTree ApplyInitialModifications(IProjectTree node)
        {
            if (!node.IsProjectRoot())
                return node;

            ProjectImageMoniker icon;
            if (!_provider.TryGetProjectImage(ProjectImageKey.ProjectRoot, out icon) || node.Icon == icon)
                return node;

            return node.SetIcon(icon);
        }
    }
}

